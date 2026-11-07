using Asp.Versioning;
using ST.Api.Common.Controllers;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.Queries;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace ST.Api.Features.IAM.Users.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/iam/auth")]
[ApiController]
public sealed class AuthController(ISender sender) : BaseController
{
    [HttpPost("invitation/accept")]
    [AllowAnonymous]
    [EnableRateLimiting("TwoFactorPolicy")]
    public async Task<IActionResult> AcceptInvitation(AcceptAccountInvitationRequest request, CancellationToken cancellationToken)
        => HandleResponse(await sender.Send(new AcceptAccountInvitationCommand(request), cancellationToken).ConfigureAwait(false));

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Login(LoginApiRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new LoginCommand(request))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [EnableRateLimiting("RefreshTokenPolicy")]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new RefreshTokenCommand(request))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var response = await sender
            .Send(new LogoutCommand())
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var response = await sender
            .Send(new GetCurrentUserCommand())
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("email-otp/send")]
    [AllowAnonymous]
    [EnableRateLimiting("TwoFactorPolicy")]
    public async Task<IActionResult> SendEmailOtp(SendEmailOtpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new SendEmailOtpCommand(request))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("email-otp/verify")]
    [AllowAnonymous]
    [EnableRateLimiting("TwoFactorPolicy")]
    public async Task<IActionResult> VerifyEmailOtp(VerifyEmailOtpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new VerifyEmailOtpCommand(request))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpGet("otp-status/{userId}")]
    [AllowAnonymous]
    [EnableRateLimiting("TwoFactorPolicy")]
    public async Task<IActionResult> GetOtpStatus(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var response = await sender
            .Send(new GetOtpStatusCommand(userId))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("password/verify")]
    [Authorize]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> VerifyPassword(VerifyPasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new VerifyPasswordCommand(request))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("password/change")]
    [Authorize]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new ChangePasswordCommand(request))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("password/reset")]
    [AllowAnonymous]
    [EnableRateLimiting("PasswordResetPolicy")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new ResetPasswordCommand(request))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("password/forgot")]
    [AllowAnonymous]
    [EnableRateLimiting("PasswordResetPolicy")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new ForgotPasswordCommand(request))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("password/reset/verify-otp")]
    [AllowAnonymous]
    [EnableRateLimiting("PasswordResetPolicy")]
    public async Task<IActionResult> VerifyPasswordResetOtp(VerifyPasswordResetOtpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new VerifyPasswordResetOtpCommand(request))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpGet("sso/login")]
    [AllowAnonymous]
    public IActionResult SsoLogin([FromServices] ST.Domain.Shared.Contracts.Common.ICurrentTenantProvider tenant, [FromQuery] string returnUrl = "/", [FromQuery] Guid? tenantId = null)
    {
        var redirectUrl = Url.Action(nameof(SsoCallback), "Auth", new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        properties.Items["workspace_tenant"] = (tenantId ?? tenant.TenantId).ToString();
        properties.Items["workspace_return"] = returnUrl;
        return Challenge(properties, "EntraId");
    }

    [HttpGet("sso/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> SsoCallback([FromServices] ST.Domain.Shared.Contracts.Common.IWorkspaceTenantOverride workspace, [FromServices] ST.Domain.Features.ControlPlane.Contracts.IControlPlaneUnitOfWork plane, [FromQuery] string returnUrl = "/")
    {
        var result = await HttpContext.AuthenticateAsync(Microsoft.AspNetCore.Identity.IdentityConstants.ExternalScheme).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return BadRequest("External authentication failed.");
        }

        if (!Guid.TryParse((result.Properties?.Items.TryGetValue("workspace_tenant", out var selectedWorkspace) == true ? selectedWorkspace : null), out var tenantId)
            || !await plane.Tenants.AnyAsync(x => x.Id == tenantId && x.Status == ST.Domain.Features.ControlPlane.Tenants.Enums.TenantStatus.Active && !x.IsDemo, HttpContext.RequestAborted).ConfigureAwait(false)) return BadRequest("Workspace is unavailable.");
        workspace.TenantId = tenantId;
        returnUrl = (result.Properties?.Items.TryGetValue("workspace_return", out var storedReturn) == true ? storedReturn : null) ?? "/";
        var claims = result.Principal.Claims;
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
            ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "Unknown";
        var nameParts = name.Split(' ', 2);
        var firstName = nameParts.Length > 0 ? nameParts[0] : "Unknown";
        var lastName = nameParts.Length > 1 ? nameParts[1] : "Unknown";

        var provider = result.Properties?.Items["LoginProvider"] ?? "EntraId";
        var providerKey = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(providerKey))
        {
            return BadRequest("Missing required claims from external provider.");
        }

        var command = new ProcessSsoLoginCommand(email, firstName, lastName, provider, providerKey);
        var response = await sender.Send(command).ConfigureAwait(false);

        if (!response.IsSuccess)
        {
            return BadRequest(response.Message);
        }

        await HttpContext.SignOutAsync(Microsoft.AspNetCore.Identity.IdentityConstants.ExternalScheme).ConfigureAwait(false);

        var uriBuilder = new UriBuilder(returnUrl);
        var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
        query["code"] = response.Data;
        uriBuilder.Query = query.ToString();

        return Redirect(uriBuilder.ToString());
    }

    [HttpPost("sso/exchange")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> ExchangeSsoCode([FromBody] ExchangeSsoCodeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new ExchangeSsoCodeCommand(request.Code))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("passkey/register/options")]
    [Authorize]
    public async Task<IActionResult> RequestPasskeyRegistrationOptions()
    {
        var response = await sender
            .Send(new RequestPasskeyRegistrationOptionsCommand())
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("passkey/register")]
    [Authorize]
    public async Task<IActionResult> RegisterPasskey([FromBody] RegisterPasskeyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new RegisterPasskeyCommand(request.AttestationResponse))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("passkey/login/options")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> RequestPasskeyLoginOptions([FromBody] RequestPasskeyLoginOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new RequestPasskeyLoginOptionsCommand(request.Username))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpPost("passkey/login")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> LoginWithPasskey([FromBody] LoginWithPasskeyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await sender
            .Send(new LoginWithPasskeyCommand(request.Username, request.CorrelationId, request.AssertionResponse))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpGet("passkeys")]
    [Authorize]
    public async Task<IActionResult> GetPasskeys()
    {
        var response = await sender
            .Send(new GetPasskeysQuery())
            .ConfigureAwait(false);

        return HandleResponse(response);
    }
}
