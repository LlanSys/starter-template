using Microsoft.EntityFrameworkCore;
using ST.Infrastructure.Contracts.Implementations.Common;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.SharedKernel.Features.ControlPlane.Demo;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Application.Features.Shared.EmailTemplates.Mappings;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.IAM.Users.Entities;
using StoredRefreshToken = ST.Domain.Features.IAM.Users.Entities.RefreshToken;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ST.SharedKernel.Extensions;
using ST.Application.Features.ControlPlane.Support;
using ST.Domain.Features.ControlPlane.Support.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.SharedKernel.Features.ControlPlane;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class RefreshToken(
    UserManager<AppUser> userManager,
    IIamUnitOfWork iamUnitOfWork,
    IJwtService jwtService,
    IClaimsService claimsService,
    ISessionService sessionService,
    IHttpContextAccessor httpContextAccessor,
    IOptions<JwtSettings> jwtSettings,
    TimeProvider timeProvider,
    ILogger<RefreshToken> logger, WorkspaceTenantOverride? workspace = null, IControlPlaneUnitOfWork? controlPlane = null, ISupportSessionGuard? supportGuard = null) : IRequestHandler<RefreshTokenCommand, AppResponse<RefreshTokenResponse>>
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public async Task<AppResponse<RefreshTokenResponse>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var previousTenant = workspace?.TenantId;

        try
        {
            var principal = jwtService.GetPrincipalFromToken(request.AccessToken, false);
            if (principal?.Identity?.IsAuthenticated != true || principal.HasClaim("temp_auth", "true"))
            {
                SecurityLogDefinitions.LogSecurityEvent(logger, "InvalidAccessTokenFormat", string.Empty, "Invalid access token format provided for refresh");
                return AppResponses.Failure<RefreshTokenResponse>("Invalid access token format");
            }

            if (Guid.TryParse(principal.FindFirstValue("tenant_id"), out var tokenTenant) && workspace is not null)
            {
                if (controlPlane is not null && !await controlPlane.Tenants.AnyAsync(x => x.Id == tokenTenant && x.Status == TenantStatus.Active, cancellationToken).ConfigureAwait(false)) return AppResponses.Failure<RefreshTokenResponse>("This workspace is not available.");
                workspace.TenantId = tokenTenant;
            }
            if (principal.HasClaim("demo_workspace", "true"))
            {
                if (workspace is null || controlPlane is null || principal.FindFirstValue("tenant_id") != DemoWorkspace.TenantId.ToString())
                    return AppResponses.Failure<RefreshTokenResponse>("The demo session cannot be refreshed.");
                workspace.TenantId = DemoWorkspace.TenantId;
                var sourceId = principal.FindFirstValue("demo_actor_id");
                var source = await userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(user => user.Id == sourceId && user.IsActive && !user.IsDeleted && user.TwoFactorEnabled, cancellationToken).ConfigureAwait(false);
                if (source is null || !await controlPlane.Tenants.AnyAsync(tenant => tenant.Id == DemoWorkspace.TenantId && tenant.IsDemo && tenant.Status == TenantStatus.Active, cancellationToken).ConfigureAwait(false))
                    return AppResponses.Failure<RefreshTokenResponse>("The demo session is no longer available.");
                var authority = await userManager.GetClaimsAsync(source).ConfigureAwait(false);
                if (!authority.Any(claim => claim.Type == "platform_staff" && claim.Value == "true") || !authority.Any(claim => claim.Type == "permission" && claim.Value == "control_plane.manage"))
                    return AppResponses.Failure<RefreshTokenResponse>("The demo session is no longer available.");
            }

            SupportGrant? supportGrant = null;
            if (principal.HasClaim(x => x.Type == "support_grant_id")) {
                supportGrant = supportGuard is null ? null : await supportGuard.ValidateAsync(principal,cancellationToken).ConfigureAwait(false);
                if (supportGrant is null) return AppResponses.Failure<RefreshTokenResponse>("The support session expired or was revoked.");
            }
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(userId))
            {
                SecurityLogDefinitions.LogSecurityEvent(logger, "InvalidAccessTokenFormat", string.Empty, "No user ID found in access token");
                return AppResponses.Failure<RefreshTokenResponse>("Invalid token: No user ID found");
            }

            var user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
            if (user == null)
            {
                SecurityLogDefinitions.LogSecurityEvent(logger, "UserNotFoundForRefresh", userId, "User not found for refresh token");
                return AppResponses.Failure<RefreshTokenResponse>("User not found");
            }

            if (!user.IsActive || user.IsDeleted)
            {
                SecurityLogDefinitions.LogSecurityEvent(logger, "InactiveUserRefreshAttempt", userId, "Refresh token attempt for inactive user");
                return AppResponses.Failure<RefreshTokenResponse>("User account is not active");
            }

            var sessionId = principal.FindFirstValue("session_id");
            if (!Guid.TryParse(sessionId, out var activeSessionId))
            {
                return AppResponses.Failure<RefreshTokenResponse>("Your session is no longer active. Please sign in again.");
            }

            var sessionValidation = await sessionService.IsSessionValidAsync(sessionId, userId, cancellationToken).ConfigureAwait(false);
            if (!sessionValidation.IsSuccess)
            {
                SecurityLogDefinitions.LogSecurityEvent(logger, "InvalidSessionRefreshAttempt", userId, sessionValidation.Message ?? "Invalid session");
                return AppResponses.Failure<RefreshTokenResponse>("Your session is no longer active. Please sign in again.");
            }

            var storedRefreshToken = await iamUnitOfWork.TokenRepository.GetRefreshTokenAsync(request.RefreshToken, userId).ConfigureAwait(false);
            if (storedRefreshToken == null)
            {
                SecurityLogDefinitions.LogSecurityEvent(logger, "RefreshTokenNotFound", userId, "Refresh token not found for user");
                return AppResponses.Failure<RefreshTokenResponse>("Invalid refresh token");
            }

            var now = timeProvider.GetUtcNow();
            if (storedRefreshToken.ExpiresAt <= now)
            {
                SecurityLogDefinitions.LogSecurityEvent(logger, "ExpiredRefreshToken", userId, "Expired refresh token used");
                await iamUnitOfWork.ExecuteInTransactionWithRetryAsync(async () =>
                {
                    await iamUnitOfWork.TokenRepository.RevokeRefreshTokenAsync(storedRefreshToken, "Token expired").ConfigureAwait(false);
                    await iamUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
                    return true;
                }).ConfigureAwait(false);

                return AppResponses.Failure<RefreshTokenResponse>("Refresh token has expired");
            }

            if (storedRefreshToken.IsRevoked)
            {
                SecurityLogDefinitions.LogSecurityEvent(logger, "RevokedRefreshToken", userId, "Revoked refresh token used");
                return AppResponses.Failure<RefreshTokenResponse>("Refresh token has been revoked");
            }

            if (storedRefreshToken.IsUsed)
            {
                SecurityLogDefinitions.LogSecurityEvent(logger, "TokenReuseDetected", userId, "Already used refresh token attempted");
                await iamUnitOfWork.ExecuteInTransactionWithRetryAsync(async () =>
                {
                    await iamUnitOfWork.TokenRepository.RevokeAllUserTokensAsync(userId, "Token reuse detected").ConfigureAwait(false);
                    await iamUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
                    return true;
                }).ConfigureAwait(false);

                return AppResponses.Failure<RefreshTokenResponse>("Refresh token has already been used");
            }

            if (storedRefreshToken.AppUserId != userId)
            {
                SecurityLogDefinitions.LogSecurityEvent(logger, "RefreshTokenUserMismatch", userId, $"Token UserId: {storedRefreshToken.AppUserId}, Request UserId: {userId}");
                return AppResponses.Failure<RefreshTokenResponse>("Token mismatch");
            }

            var userClaims = await claimsService.GetUserClaimsAsync(user, activeSessionId).ConfigureAwait(false);
            if (supportGrant is not null) userClaims = userClaims.Where(x => x.Type != ClaimTypes.Role && x.Type != "platform_staff" && (x.Type != "permission" || supportGrant.PermissionKeys.Contains(x.Value,StringComparer.OrdinalIgnoreCase))).ToList();
            if (PlatformAccess.HasMfa(principal)) userClaims.Add(new Claim("amr", "mfa"));
            if (principal.HasClaim("administrator_mfa", "true"))
                userClaims.Add(new Claim("administrator_mfa", "true"));
            if (!userClaims.Any())
            {
                ServiceLogDefinitions.LogFailedToGetUserClaims(logger, userId);
                return AppResponses.Failure<RefreshTokenResponse>("Could not retrieve user claims");
            }

            var newAccessTokenResponse = await jwtService.CreateTokenAsync(userClaims).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(newAccessTokenResponse))
            {
                ServiceLogDefinitions.LogFailedToGenerateAccessToken(logger, userId);
                return AppResponses.Failure<RefreshTokenResponse>("Could not generate new access token");
            }

            var newRefreshTokenResponse = jwtService.CreateRefreshToken();
            if (string.IsNullOrWhiteSpace(newRefreshTokenResponse))
            {
                ServiceLogDefinitions.LogFailedToGenerateRefreshToken(logger, userId);
                return AppResponses.Failure<RefreshTokenResponse>("Could not generate new refresh token");
            }

            var tokenExpiry = jwtService.GetTokenExpiry(newAccessTokenResponse);
            if (tokenExpiry == default)
            {
                ServiceLogDefinitions.LogFailedToGetTokenExpiry(logger, userId);
                return AppResponses.Failure<RefreshTokenResponse>("Could not determine token expiry");
            }

            var newRefreshTokenEntity = StoredRefreshToken.Create(
                now,
                userId,
                newRefreshTokenResponse,
                now.AddHours(_jwtSettings.RefreshTokenExpiryHours),
                userId,
                httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                storedRefreshToken.TokenFamily);
            var refreshTokenExpiresAt = newRefreshTokenEntity.ExpiresAt;

            await iamUnitOfWork.ExecuteInTransactionWithRetryAsync(async () =>
            {
                await iamUnitOfWork.TokenRepository.MarkTokenAsUsedAsync(storedRefreshToken).ConfigureAwait(false);
                await iamUnitOfWork.TokenRepository.AddRefreshTokenAsync(newRefreshTokenEntity).ConfigureAwait(false);
                await iamUnitOfWork.TokenRepository.CleanupExpiredTokensAsync(userId).ConfigureAwait(false);
                await iamUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }).ConfigureAwait(false);

            user.MarkUpdated(now, user.Id);
            await userManager.UpdateAsync(user).ConfigureAwait(false);

            var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);

            var userInfo = user.ToAppUserResponse(roles);

            ServiceLogDefinitions.LogTokenRefreshed(logger, userId);

            return AppResponses.Success("Token refreshed successfully", new RefreshTokenResponse(
                newAccessTokenResponse,
                newRefreshTokenResponse,
                userId,
                sessionId,
                tokenExpiry,
                refreshTokenExpiresAt,
                userInfo,
                userClaims.Select(claim => new ClaimResponse(claim.Type, claim.Value)).ToList()));
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogTokenRefreshError(logger, ex, request.AccessToken?.Length ?? 0, !string.IsNullOrWhiteSpace(request.RefreshToken));

            return AppResponses.Failure<RefreshTokenResponse>("Unable to refresh token. Please sign in again.");
        }
        finally
        {
            if (workspace is not null) workspace.TenantId = previousTenant;
        }

    }
}
