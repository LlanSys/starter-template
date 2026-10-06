using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.SharedKernel.Extensions;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Application.Features.Shared.EmailTemplates.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Collections.ObjectModel;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class Login(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IHttpContextAccessor httpContextAccessor,
    IJwtService jwtService,
    IClaimsService claimsService,
    IServiceManager serviceManager,
    IIamUnitOfWork iamUnitOfWork,
    IOptions<JwtSettings> jwtSettings,
    IOptions<MfaSettings> mfaSettings,
    ILogger<Login> logger) : IRequestHandler<LoginCommand, AppResponse<LoginResponse>>
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;
    private readonly MfaSettings _mfaSettings = mfaSettings.Value;

    public async Task<AppResponse<LoginResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var loginRequest = command.LoginRequest;

        AppUser? user = null;

        try
        {
            user = await userManager.Users
                .FirstOrDefaultAsync(u => u.UserName == loginRequest.UserName, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                ServiceLogDefinitions.LogLoginError(logger, loginRequest.UserName, new AuthenticationException("Invalid user"));
                return AppResponses.Failure<LoginResponse>("Invalid User Name or password.");
            }

            if (!user.IsActive || user.IsDeleted)
            {
                ServiceLogDefinitions.LogLoginError(logger, user.UserName ?? string.Empty, new AuthenticationException("User account is disabled"));
                return AppResponses.Failure<LoginResponse>("This account is inactive. Please contact support.");
            }

            var emailConfirmed = await userManager.IsEmailConfirmedAsync(user).ConfigureAwait(false);
            if (!emailConfirmed)
            {
                ServiceLogDefinitions.LogLoginError(logger, user.UserName ?? string.Empty, new AuthenticationException("Unconfirmed email"));
                return AppResponses.Failure<LoginResponse>("Please confirm your email before logging in.");
            }

            // Use the username overload to avoid introducing a second tracked AppUser instance
            var signInResult = await signInManager
                .PasswordSignInAsync(loginRequest.UserName, loginRequest.Password, loginRequest.RememberMe, true)
                .ConfigureAwait(false);

            if (signInResult.IsLockedOut)
            {
                ServiceLogDefinitions.LogLoginError(logger, user.UserName ?? string.Empty, new AuthenticationException("Account locked"));
                return AppResponses.Failure<LoginResponse>("Your account is locked due to multiple failed login attempts. Please reset your password or contact support.");
            }

            if (signInResult.IsNotAllowed)
            {
                ServiceLogDefinitions.LogLoginError(logger, user.UserName ?? string.Empty, new AuthenticationException("Sign in not allowed"));
                return AppResponses.Failure<LoginResponse>("Sign in not allowed. Please contact support.");
            }

            if (!signInResult.Succeeded && !signInResult.RequiresTwoFactor)
            {
                ServiceLogDefinitions.LogLoginError(logger, user.UserName ?? string.Empty, new AuthenticationException("Sign in failed"));
                return AppResponses.Failure<LoginResponse>("Invalid login attempt.");
            }

            var twoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false);
            if (!twoFactorEnabled)
            {
                ServiceLogDefinitions.LogUsingTempSecret(logger, user.Id);
            }

            var appUserResponse = user.ToAppUserResponse();

            var rememberedTwoFactorClient = twoFactorEnabled
                && await signInManager.IsTwoFactorClientRememberedAsync(user).ConfigureAwait(false);
            var requiresTwoFactorChallenge = signInResult.RequiresTwoFactor || (twoFactorEnabled && !rememberedTwoFactorClient);

            if (requiresTwoFactorChallenge)
            {
                var tempClaims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.Id),
                    new(ClaimTypes.Name, user.UserName ?? string.Empty),
                    new("temp_auth", "true")
                };

                var tempToken = await jwtService.CreateTempTokenAsync(tempClaims, TimeSpan.FromMinutes(10)).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(tempToken))
                {
                    ServiceLogDefinitions.LogFailedToGenerateAccessToken(logger, user.Id);
                    return AppResponses.Failure<LoginResponse>("Could not generate temporary authentication token");
                }

                var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
                var userInfoWith2FA = appUserResponse with { Roles = [.. roles] };


                return AppResponses.Success("Two-factor authentication required", new LoginResponse(
                    user.Id,
                    user.FirstName ?? string.Empty,
                    user.LastName ?? string.Empty,
                    user.Email ?? string.Empty,
                    true,
                    false,
                    false,
                    tempToken,
                    string.Empty,
                    null,
                    DateTimeOffset.UtcNow.AddMinutes(10),
                    userInfoWith2FA,
                    tempClaims.ToClaimResponses()));
            }

            var sessionId = Guid.CreateVersion7();
            var sessionCreationResult = await serviceManager.SessionService.CreateSessionAsync(
                user.Id,
                sessionId,
                httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown",
                httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown",
                loginRequest.DeviceFingerprint ?? "unknown",
                cancellationToken).ConfigureAwait(false);

            if (!sessionCreationResult.IsSuccess)
            {
                ServiceLogDefinitions.LogFailedToCreateUserSession(logger, user.Id);
                return AppResponses.Failure<LoginResponse>("Could not establish a user session.");
            }

            var activeSessionId = sessionCreationResult.Data;
            if (activeSessionId == Guid.Empty)
            {
                ServiceLogDefinitions.LogFailedToCreateUserSession(logger, user.Id);
                return AppResponses.Failure<LoginResponse>("Could not establish a user session.");
            }

            var userClaims = await claimsService.GetUserClaimsAsync(user, activeSessionId).ConfigureAwait(false);
            if (!userClaims.Any())
            {
                ServiceLogDefinitions.LogFailedToGetUserClaims(logger, user.Id);
                return AppResponses.Failure<LoginResponse>("Could not retrieve user claims");
            }

            var tokenResponse = await jwtService.CreateTokenAsync(userClaims).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(tokenResponse))
            {
                ServiceLogDefinitions.LogFailedToGenerateAccessToken(logger, user.Id);
                return AppResponses.Failure<LoginResponse>("Could not generate authentication token");
            }

            var refreshToken = jwtService.CreateRefreshToken();
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                ServiceLogDefinitions.LogFailedToGenerateRefreshToken(logger, user.Id);
                return AppResponses.Failure<LoginResponse>("Could not generate refresh token");
            }

            var refreshTokenEntity = ST.Domain.Features.IAM.Users.Entities.RefreshToken.Create(
                user.Id,
                refreshToken,
                DateTimeOffset.UtcNow.AddHours(_jwtSettings.RefreshTokenExpiryHours),
                user.Id,
                httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString());

            var jwtHandler = new JwtSecurityTokenHandler();
            var jwt = jwtHandler.ReadJwtToken(tokenResponse);
            var tokenExpiry = jwt.ValidTo;

            if (!jwtService.IsTokenValid(tokenResponse))
            {
                var tokenException = new SecurityTokenException("Token validation failed");
                ServiceLogDefinitions.LogInvalidTokenWithException(logger, tokenException);
                return AppResponses.Failure<LoginResponse>("Invalid authentication token");
            }

            var rolesResponse = await userManager.GetRolesAsync(user).ConfigureAwait(false);
            var userRoles = new Collection<string>(rolesResponse.ToList());
            var mfaEnrollmentRequired = IsMfaEnrollmentRequired(twoFactorEnabled, rolesResponse);
            // Reload the user via UserManager to ensure we operate on the EF-tracked instance
            var trackedUser = await userManager.FindByIdAsync(user.Id).ConfigureAwait(false);
            if (trackedUser == null)
            {
                ServiceLogDefinitions.LogLoginError(logger, user.UserName ?? string.Empty, new InvalidOperationException("User disappeared from store before update"));
                return AppResponses.Failure<LoginResponse>("Could not complete login.");
            }

            trackedUser.RecordSuccessfulLogin();
            var finalAppUserResponse = appUserResponse with { Roles = userRoles, LastLoginAt = trackedUser.LastLoginAt };

            var userUpdateResult = await userManager.UpdateAsync(trackedUser).ConfigureAwait(false);
            if (!userUpdateResult.Succeeded)
            {
                var updateError = string.Join("; ", userUpdateResult.Errors.Select(e => e.Description));
                ServiceLogDefinitions.LogLoginError(logger, user.UserName ?? string.Empty, new InvalidOperationException(updateError));
                return AppResponses.Failure<LoginResponse>("Could not complete login.");
            }


            await iamUnitOfWork.ExecuteInTransactionWithRetryAsync(async () =>
            {
                await iamUnitOfWork.TokenRepository.AddRefreshTokenAsync(refreshTokenEntity).ConfigureAwait(false);
                await iamUnitOfWork.TokenRepository.CleanupExpiredTokensAsync(user.Id).ConfigureAwait(false);
                await iamUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }).ConfigureAwait(false);

            return AppResponses.Success("Login successful", new LoginResponse(
                user.Id,
                user.FirstName ?? string.Empty,
                user.LastName ?? string.Empty,
                user.Email ?? string.Empty,
                false,
                false,
                true,
                tokenResponse,
                refreshToken,
                activeSessionId.ToString(),
                tokenExpiry,
                finalAppUserResponse,
                userClaims.ToClaimResponses(),
                mfaEnrollmentRequired));
        }
        catch (InvalidOperationException ex) when (ex.Message != null && (ex.Message.Contains("already being tracked") || ex.Message.Contains("cannot be tracked")))
        {
            // Detailed telemetry for duplicate-tracking EF Core errors
            var remoteIp = httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown";
            ServiceLogDefinitions.LogDuplicateAppUserTracking(logger, ex, user?.Id, loginRequest.UserName, remoteIp, userAgent);
            ServiceLogDefinitions.LogLoginError(logger, loginRequest.UserName, ex);
            throw;
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogLoginError(logger, loginRequest.UserName, ex);
            throw;
        }
    }

    private bool IsMfaEnrollmentRequired(bool twoFactorEnabled, IEnumerable<string> roles)
    {
        if (!_mfaSettings.Enabled || !_mfaSettings.EnforceEnrollment || twoFactorEnabled)
        {
            return false;
        }

        return roles.Any(role => _mfaSettings.RequiredRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }
}
