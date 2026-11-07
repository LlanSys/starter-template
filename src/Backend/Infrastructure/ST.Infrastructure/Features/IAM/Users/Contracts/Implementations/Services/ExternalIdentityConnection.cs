using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;

namespace ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Services;

internal sealed partial class ExternalIdentityConnection(
    UserManager<AppUser> users, SignInManager<AppUser> signIn, IHttpContextAccessor http, ICurrentTenantProvider scope,
    SsoCodeStore codes, ILogger<ExternalIdentityConnection> logger) : IExternalIdentityConnection
{
    public async Task<AppResponse<string>> StartAsync(ConnectExternalIdentityRequest request, CancellationToken cancellationToken)
    {
        var principal = http.HttpContext?.User;
        var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || principal?.FindFirstValue("session_id") is null
            || principal.HasClaim(claim => claim.Type is "support_grant_id" or "demo_workspace"))
        {
            return AppResponses.Failure<string>("Sign in to connect a provider.");
        }

        var user = await users.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null || user.TenantId != scope.TenantId || !user.IsActive || user.IsDeleted || user.RequiresAdministratorSetup
            || !(await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true).ConfigureAwait(false)).Succeeded)
        {
            return AppResponses.Failure<string>("Confirm your current password to connect a provider.");
        }

        return AppResponses.Success(await codes.IssueAsync(user, request.Provider, cancellationToken).ConfigureAwait(false));
    }

    public async Task<AppResponse<bool>> ConnectAsync(string ticket, string provider, string providerKey, CancellationToken cancellationToken)
    {
        var user = await codes.ConsumeAsync(ticket, provider, cancellationToken).ConfigureAwait(false);
        if (user is null || user.RequiresAdministratorSetup)
        {
            return AppResponses.Failure<bool>("This connection request expired or is unavailable.");
        }

        var linked = await users.FindByLoginAsync(provider, providerKey).ConfigureAwait(false);
        if (linked is not null && linked.Id != user.Id)
        {
            return AppResponses.Failure<bool>("This external identity is already connected.");
        }

        if (linked is null && !(await users.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider)).ConfigureAwait(false)).Succeeded)
        {
            return AppResponses.Failure<bool>("This external identity could not be connected.");
        }

        LogProviderConnected(logger, user.Id, user.TenantId, provider);
        return AppResponses.Success(true);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Connected {Provider} to account {UserId} in workspace {TenantId}.")]
    private static partial void LogProviderConnected(ILogger logger, string userId, Guid tenantId, string provider);
}
