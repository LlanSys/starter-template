using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using ST.Api.Common.Controllers;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Configuration;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using ST.SharedKernel.Features.IAM.Users.Dtos;

namespace ST.Api.Features.IAM.Users.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/iam/auth/sso")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ExternalSignInController(
    ISender sender, IAuthenticationSchemeProvider schemes, IControlPlaneUnitOfWork plane,
    IWorkspaceTenantOverride workspace, IAdministratorInvitationService invitations, IExternalIdentityConnection connections,
    IOptions<ExternalSignInSettings> configuration) : BaseController
{
    [HttpGet("providers")]
    [AllowAnonymous]
    public async Task<IActionResult> Providers()
    {
        var enabled = (await schemes.GetAllSchemesAsync().ConfigureAwait(false))
            .Where(scheme => IsProvider(scheme.Name))
            .Select(scheme => new SsoProviderResponse(scheme.Name, scheme.Name == "Google" ? "Google" : "Microsoft"))
            .ToArray();
        return HandleResponse(AppResponses.Success(enabled));
    }

    [HttpGet("login")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public Task<IActionResult> SsoLogin([FromQuery] Guid tenantId, [FromQuery] string returnUrl,
        [FromQuery] string provider, CancellationToken cancellationToken)
    {
        return StartAsync(tenantId, returnUrl, provider, null, null, null, cancellationToken);
    }

    [HttpPost("accept")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public Task<IActionResult> AcceptWithProvider([FromForm] Guid tenantId, [FromForm] string returnUrl,
        [FromForm] string provider, [FromForm] Guid invitationId, [FromForm] string invitationToken, CancellationToken cancellationToken)
    {
        return StartAsync(tenantId, returnUrl, provider, invitationId, invitationToken, null, cancellationToken);
    }

    [HttpPost("connect")]
    [Authorize]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> PrepareConnection(ConnectExternalIdentityRequest request, CancellationToken cancellationToken)
    {
        if (!await IsEnabledAsync(request.Provider).ConfigureAwait(false))
        {
            return BadRequest("This provider is unavailable.");
        }

        return HandleResponse(await connections.StartAsync(request, cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("connect/start")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public Task<IActionResult> StartConnection([FromForm] Guid tenantId, [FromForm] string returnUrl,
        [FromForm] string provider, [FromForm] string ticket, CancellationToken cancellationToken)
    {
        return StartAsync(tenantId, returnUrl, provider, null, null, ticket, cancellationToken);
    }

    [HttpGet("callback")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> SsoCallback(CancellationToken cancellationToken)
    {
        var external = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme).ConfigureAwait(false);
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme).ConfigureAwait(false);
        if (!external.Succeeded || external.Principal is null || external.Properties is null)
        {
            return BadRequest("External sign-in could not be verified.");
        }

        var items = external.Properties.Items;
        string? ReadState(string key) => items.TryGetValue(key, out var value) ? value : null;
        if (!Guid.TryParse(ReadState("st_tenant"), out var tenantId)
            || !TryReturnUrl(ReadState("st_return"), out var returnUrl)
            || !await IsWorkspaceActiveAsync(tenantId, cancellationToken).ConfigureAwait(false)
            || ReadState("st_provider") is not { } provider
            || !await IsEnabledAsync(provider).ConfigureAwait(false))
        {
            return BadRequest("External sign-in could not be verified for this workspace.");
        }

        workspace.TenantId = tenantId;
        var issuer = external.Principal.FindFirstValue("st_validated_issuer");
        var subject = external.Principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject))
        {
            return Redirect(QueryHelpers.AddQueryString(returnUrl, "error", "identity"));
        }

        var providerKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{issuer}\n{subject}")));
        if (ReadState("st_connection") is { } ticket)
        {
            var linked = await connections.ConnectAsync(ticket, provider, providerKey, cancellationToken).ConfigureAwait(false);
            return Redirect(linked.IsSuccess ? QueryHelpers.AddQueryString(returnUrl, "connected", "1")
                : QueryHelpers.AddQueryString(returnUrl, "error", "connection"));
        }

        var invitationId = Guid.TryParse(ReadState("st_invitation"), out var parsed) ? (Guid?)parsed : null;
        var verifiedEmail = provider == "Google" && !string.Equals(external.Principal.FindFirstValue("email_verified"), "true", StringComparison.OrdinalIgnoreCase)
            ? null : external.Principal.FindFirstValue("email");
        var result = await sender.Send(new ProcessSsoLoginCommand(provider, providerKey,
            invitationId, ReadState("st_invitation_token"), verifiedEmail), cancellationToken).ConfigureAwait(false);
        return Redirect(result.IsSuccess && result.Data is not null
            ? QueryHelpers.AddQueryString(returnUrl, "code", result.Data)
            : QueryHelpers.AddQueryString(returnUrl, "error", "identity"));
    }

    private async Task<IActionResult> StartAsync(Guid tenantId, string returnUrl, string provider,
        Guid? invitationId, string? invitationToken, string? ticket, CancellationToken cancellationToken)
    {
        if (!TryReturnUrl(returnUrl, out var destination)
            || !await IsEnabledAsync(provider).ConfigureAwait(false)
            || !await IsWorkspaceActiveAsync(tenantId, cancellationToken).ConfigureAwait(false))
        {
            return BadRequest("External sign-in is unavailable for this workspace.");
        }

        workspace.TenantId = tenantId;
        if (invitationId.HasValue && (string.IsNullOrWhiteSpace(invitationToken)
            || !(await invitations.InspectAsync(new InspectAdministratorInvitationRequest(invitationId.Value, invitationToken),
                cancellationToken).ConfigureAwait(false)).IsSuccess))
        {
            return BadRequest("The invitation is unavailable.");
        }

        var callback = Url.Action(nameof(SsoCallback), values: new { version = "1" });
        if (string.IsNullOrWhiteSpace(callback))
        {
            return BadRequest("External sign-in is unavailable.");
        }

        var properties = new AuthenticationProperties { RedirectUri = callback };
        properties.Items["st_tenant"] = tenantId.ToString();
        properties.Items["st_return"] = destination;
        properties.Items["st_provider"] = provider;
        if (invitationId.HasValue)
        {
            properties.Items["st_invitation"] = invitationId.Value.ToString();
            properties.Items["st_invitation_token"] = invitationToken;
        }
        if (!string.IsNullOrEmpty(ticket))
        {
            properties.Items["st_connection"] = ticket;
        }
        return Challenge(properties, provider);
    }

    private bool TryReturnUrl(string? candidate, out string returnUrl)
    {
        returnUrl = string.Empty;
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) || uri.AbsolutePath != "/iam/sso-callback"
            || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        var origin = uri.GetLeftPart(UriPartial.Authority);
        if (!configuration.Value.ClientOrigins.Any(allowed => string.Equals(allowed.TrimEnd('/'), origin, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        returnUrl = uri.AbsoluteUri;
        return true;
    }

    private Task<bool> IsWorkspaceActiveAsync(Guid tenantId, CancellationToken cancellationToken) =>
        plane.Tenants.AnyAsync(tenant => tenant.Id == tenantId && tenant.Status == TenantStatus.Active && !tenant.IsDemo, cancellationToken);

    private async Task<bool> IsEnabledAsync(string provider) =>
        IsProvider(provider) && await schemes.GetSchemeAsync(provider).ConfigureAwait(false) is not null;

    private static bool IsProvider(string name) => name is "EntraId" or "Google";
}
