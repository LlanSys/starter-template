using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.Banking.Customers.Mappings;
using ST.Application.Features.HR.Employees.Mappings;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Application.Features.Shared.EmailTemplates.Mappings;
using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.IAM.Users.Entities;
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

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class RefreshToken(
    UserManager<AppUser> userManager,
    IIamUnitOfWork iamUnitOfWork,
    IJwtService jwtService,
    IClaimsService claimsService,
    ISessionService sessionService,
    IHttpContextAccessor httpContextAccessor,
    IOptions<JwtSettings> jwtSettings,
    ILogger<RefreshToken> logger) : IRequestHandler<RefreshTokenCommand, AppResponse<RefreshTokenResponse>>
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public async Task<AppResponse<RefreshTokenResponse>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        try
        {
            var principal = jwtService.GetPrincipalFromToken(request.AccessToken, false);
            if (principal == null)
            {
                SecurityLogDefinitions.LogSecurityEvent(logger, "InvalidAccessTokenFormat", string.Empty, "Invalid access token format provided for refresh");
                return AppResponses.Failure<RefreshTokenResponse>("Invalid access token format");
            }

            var userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
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
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                var sessionValidation = await sessionService.IsSessionValidAsync(sessionId, userId, cancellationToken).ConfigureAwait(false);
                if (!sessionValidation.IsSuccess)
                {
                    SecurityLogDefinitions.LogSecurityEvent(logger, "InvalidSessionRefreshAttempt", userId, sessionValidation.Message ?? "Invalid session");
                    return AppResponses.Failure<RefreshTokenResponse>("Your session is no longer active. Please sign in again.");
                }
            }

            var storedRefreshToken = await iamUnitOfWork.TokenRepository.GetRefreshTokenAsync(request.RefreshToken, userId).ConfigureAwait(false);
            if (storedRefreshToken == null)
            {
                SecurityLogDefinitions.LogSecurityEvent(logger, "RefreshTokenNotFound", userId, "Refresh token not found for user");
                return AppResponses.Failure<RefreshTokenResponse>("Invalid refresh token");
            }

            if (storedRefreshToken.ExpiresAt <= DateTimeOffset.UtcNow)
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

            Guid? activeSessionId = Guid.TryParse(sessionId, out var parsedSessionId) ? parsedSessionId : null;
            var userClaims = await claimsService.GetUserClaimsAsync(user, activeSessionId).ConfigureAwait(false);
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

            var newRefreshTokenEntity = ST.Domain.Features.IAM.Users.Entities.RefreshToken.Create(
                userId,
                newRefreshTokenResponse,
                DateTimeOffset.UtcNow.AddHours(_jwtSettings.RefreshTokenExpiryHours),
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

            user.MarkUpdated(user.Id);
            await userManager.UpdateAsync(user).ConfigureAwait(false);

            var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
            var twoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false);

            var userInfo = new AppUserResponse(
                user.Id,
                user.UserName ?? string.Empty,
                user.FirstName ?? string.Empty,
                user.LastName ?? string.Empty,
                $"{user.FirstName ?? string.Empty} {user.LastName ?? string.Empty}".Trim(),
                user.PhoneNumber,
                user.NationalId,
                user.Email ?? string.Empty,
                user.Gender.ToDisplayString(),
                ProfilePictureUrlMapping.ToCurrentUserRoute(user.ProfilePictureUrl),
                user.IsActive,
                twoFactorEnabled,
                user.RequirePasswordChange,
                user.CreatedAt,
                user.LastLoginAt,
                [.. roles],
                user.TenantId,
                user.EmployeeId,
                user.CustomerId);

            ServiceLogDefinitions.LogTokenRefreshed(logger, userId);

            return AppResponses.Success("Token refreshed successfully", new RefreshTokenResponse(
                newAccessTokenResponse,
                newRefreshTokenResponse,
                userId,
                sessionId,
                tokenExpiry,
                refreshTokenExpiresAt,
                userInfo,
                userClaims));
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogTokenRefreshError(logger, ex, request.AccessToken?.Length ?? 0, !string.IsNullOrWhiteSpace(request.RefreshToken));

            return AppResponses.Failure<RefreshTokenResponse>("Unable to refresh token. Please sign in again.");
        }
    }
}
