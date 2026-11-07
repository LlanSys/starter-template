using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.SharedKernel.Extensions;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Application.Features.Shared.EmailTemplates.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.IAM.Users.Entities;
using StoredRefreshToken = ST.Domain.Features.IAM.Users.Entities.RefreshToken;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Services;
using System.Security.Claims;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OtpNet;
using System.Security.Cryptography;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class VerifyTotpCode(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IClaimsService claimsService,
    IJwtService jwtService,
    IDistributedCache cache,
    IEncryptionService encryptionService,
    IIamUnitOfWork iamUnitOfWork,
    ISessionService sessionService,
    IHttpContextAccessor httpContextAccessor,
    IOptions<JwtSettings> jwtSettings,
    ILogger<VerifyTotpCode> logger, SignInChallengeFactory challengeFactory, TimeProvider timeProvider) : IRequestHandler<VerifyOtpCommand, AppResponse<VerifyOtpResponse>>
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public async Task<AppResponse<VerifyOtpResponse>> Handle(VerifyOtpCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        try
        {
            var user = await userManager.FindByIdAsync(request.UserId).ConfigureAwait(false);
            if (user == null || !user.IsActive || user.IsDeleted || !user.EmailConfirmed)
            {
                ServiceLogDefinitions.Log2FAVerificationAttemptNonExistentUser(logger, request.UserId);
                return AppResponses.Failure<VerifyOtpResponse>("User not found");
            }

            var proof = challengeFactory.Validate(user, request.ChallengeToken);
            var firstFactor = proof is not null && SignInChallengeFactory.IsFirstFactorChallenge(proof, user);
            var enrolled = user.TwoFactorEnabled;
            TotpVerificationResult verification;

            var tempSecret = await iamUnitOfWork.TempTotpSecretRepository
                .GetValidTempSecretByUserIdAsync(user.Id)
                .ConfigureAwait(false);

            if (tempSecret != null && !firstFactor)
            {
                var enrollmentSession = httpContextAccessor.HttpContext?.User;
                if (enrollmentSession?.FindFirstValue("session_id") is null
                    || enrollmentSession.FindFirstValue(ClaimTypes.NameIdentifier) != user.Id
                    || enrollmentSession.FindFirstValue("tenant_id") != user.TenantId.ToString())
                {
                    return AppResponses.Failure<VerifyOtpResponse>("Sign in before verifying authenticator setup.");
                }

                ServiceLogDefinitions.LogUsingTempSecret(logger, user.Id);
                var tempSecretDecryption = TryDecryptTotpSecret(user, tempSecret.EncryptedSecret);

                if (tempSecretDecryption.IsUnavailable)
                {
                    return AppResponses.Failure<VerifyOtpResponse>(tempSecretDecryption.Message!);
                }

                verification = VerifyTotp(tempSecretDecryption.Secret!, request.Code)
                    ? TotpVerificationResult.Valid()
                    : TotpVerificationResult.Invalid();

                if (verification.IsValid)
                {
                    await iamUnitOfWork.AppUserTotpSecretRepository.DeactivateUserSecretsAsync(user.Id).ConfigureAwait(false);

                    var newSecret = AppUserTotpSecret.Create(
                        user.Id,
                        tempSecret.EncryptedSecret,
                        user.Id);

                    await iamUnitOfWork.AppUserTotpSecretRepository.CreateAsync(newSecret, cancellationToken).ConfigureAwait(false);
                    await iamUnitOfWork.TempTotpSecretRepository.DeleteAsync(tempSecret.Id, cancellationToken).ConfigureAwait(false);
                    await iamUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

                    var enableResult = await userManager.SetTwoFactorEnabledAsync(user, true).ConfigureAwait(false);
                    if (!enableResult.Succeeded)
                    {
                        ServiceLogDefinitions.LogTotpSetupInitiationError(logger, user.Id, new InvalidOperationException("Identity rejected MFA enrollment."));
                        return AppResponses.Failure<VerifyOtpResponse>("Could not enable two-factor authentication. Please try again.");
                    }

                    ServiceLogDefinitions.LogOtpEnabled(logger, user.Id);
                }
            }
            else
            {
                if (!firstFactor)
                {
                    return AppResponses.Failure<VerifyOtpResponse>("Sign in again before verifying your authenticator.");
                }

                verification = await VerifyUserTotpAsync(user, request.Code, cancellationToken).ConfigureAwait(false);
                if (verification.IsUnavailable)
                {
                    return AppResponses.Failure<VerifyOtpResponse>(verification.Message!);
                }
            }

            if (!verification.IsValid)
            {
                ServiceLogDefinitions.LogInvalidOtpCode(logger, request.UserId);
                return AppResponses.Failure<VerifyOtpResponse>("Invalid verification code. Please try again.");
            }

            var requestedSessionId = Guid.CreateVersion7();
            var sessionCreation = await sessionService.CreateSessionAsync(
                user.Id,
                requestedSessionId,
                httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown",
                httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown",
                request.DeviceFingerprint ?? "unknown",
                cancellationToken).ConfigureAwait(false);

            if (!sessionCreation.IsSuccess || sessionCreation.Data == Guid.Empty)
            {
                ServiceLogDefinitions.LogFailedToCreateUserSession(logger, user.Id);
                return AppResponses.Failure<VerifyOtpResponse>("Could not establish a user session.");
            }

            var activeSessionId = sessionCreation.Data;
            var userClaims = await claimsService.GetUserClaimsAsync(user, activeSessionId).ConfigureAwait(false);
            if (!userClaims.Any())
            {
                return AppResponses.Failure<VerifyOtpResponse>("Could not retrieve user claims");
            }

            userClaims.Add(new Claim("amr", "mfa"));
            if (user.RequiresAdministratorSetup && enrolled && firstFactor)
            {
                userClaims.Add(new Claim("administrator_mfa", "true"));
            }
            var tokenResponse = await jwtService.CreateTokenAsync(userClaims).ConfigureAwait(false);
            var tokenExpiry = jwtService.GetTokenExpiry(tokenResponse);
            var refreshToken = jwtService.CreateRefreshToken();
            var now = timeProvider.GetUtcNow();
            var refreshTokenEntity = StoredRefreshToken.Create(
                now,
                user.Id,
                refreshToken,
                now.AddHours(_jwtSettings.RefreshTokenExpiryHours),
                user.Id,
                httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString());

            user.RecordSuccessfulLogin(now);
            await userManager.UpdateAsync(user).ConfigureAwait(false);
            await iamUnitOfWork.ExecuteInTransactionWithRetryAsync(async () =>
            {
                await iamUnitOfWork.TokenRepository.AddRefreshTokenAsync(refreshTokenEntity).ConfigureAwait(false);
                await iamUnitOfWork.TokenRepository.CleanupExpiredTokensAsync(user.Id).ConfigureAwait(false);
                await iamUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }).ConfigureAwait(false);

            if (request.RememberDevice)
                await signInManager.RememberTwoFactorClientAsync(user).ConfigureAwait(false);

            await signInManager.SignInAsync(user, request.RememberMe).ConfigureAwait(false);

            var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);

            var appUserResponse = user.ToAppUserResponse(roles);

            var claimsResponse = userClaims.Select(c => new UserClaimsResponse
            {
                Type = c.Type,
                Value = c.Value,
                ValueType = c.ValueType,
                Issuer = c.Issuer,
                OriginalIssuer = c.OriginalIssuer,
                Properties = c.Properties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            }).ToList();

            return AppResponses.Success("OTP verification successful",
                new VerifyOtpResponse(
                    tokenResponse,
                    refreshToken ?? string.Empty,
                    user.Id,
                    activeSessionId.ToString(),
                    true,
                    tokenExpiry,
                    appUserResponse,
                    claimsResponse));
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogErrorVerifyingPassword(logger, ex);
            throw;
        }
    }

    private async Task<TotpVerificationResult> VerifyUserTotpAsync(AppUser user, string code, CancellationToken ct)
    {
        var userId = user.Id;
        var attemptKey = CacheKeys.TotpAttempts(userId);
        var attemptsStr = await cache.GetStringAsync(attemptKey, ct).ConfigureAwait(false);
        var attempts = int.TryParse(attemptsStr, out var a) ? a : 0;

        if (attempts >= 3)
            return TotpVerificationResult.Invalid();

        var secretEntity = await iamUnitOfWork.AppUserTotpSecretRepository.GetActiveSecretByUserIdAsync(userId).ConfigureAwait(false);
        if (secretEntity == null) return TotpVerificationResult.Invalid();

        var secretDecryption = TryDecryptTotpSecret(user, secretEntity.EncryptedSecret);

        if (secretDecryption.IsUnavailable)
        {
            return TotpVerificationResult.Unavailable(secretDecryption.Message!);
        }

        var isValid = VerifyTotp(secretDecryption.Secret!, code);

        if (!isValid)
        {
            attempts++;
            await cache.SetStringAsync(attemptKey, attempts.ToString(), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) }, ct).ConfigureAwait(false);
            return TotpVerificationResult.Invalid();
        }

        await cache.RemoveAsync(attemptKey, ct).ConfigureAwait(false);
        secretEntity.MarkAsUsed();

        await iamUnitOfWork.AppUserTotpSecretRepository.UpdateAsync(secretEntity, ct).ConfigureAwait(false);
        await iamUnitOfWork.CompleteAsync(ct).ConfigureAwait(false);

        return TotpVerificationResult.Valid();
    }

    private TotpSecretDecryptionResult TryDecryptTotpSecret(AppUser user, string encryptedSecret)
    {
        try
        {
            return TotpSecretDecryptionResult.Decrypted(encryptionService.Decrypt(encryptedSecret));
        }
        catch (CryptographicException ex)
        {
            ServiceLogDefinitions.LogTotpSecretDecryptionFailure(logger, user.Id, ex);
            return TotpSecretDecryptionResult.Unavailable(
                "Authenticator verification is temporarily unavailable. Please try again or contact support.");
        }
    }

    private bool VerifyTotp(string secret, string code, int windowSize = 2)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code) || code.Length != 6)
        {
            return false;
        }

        try
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(secretBytes, step: 30, mode: OtpHashMode.Sha1, totpSize: 6);
            var window = new VerificationWindow(windowSize, windowSize);
            return totp.VerifyTotp(timeProvider.GetUtcNow().UtcDateTime, code.Trim(), out _, window);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogTotpPlainTextCodeVerificationError(logger, ex);
            return false;
        }
    }

    private readonly record struct TotpVerificationResult(bool IsValid, bool IsUnavailable, string? Message)
    {
        public static TotpVerificationResult Valid() => new(true, false, null);

        public static TotpVerificationResult Invalid() => new(false, false, null);

        public static TotpVerificationResult Unavailable(string message) => new(false, true, message);
    }

    private readonly record struct TotpSecretDecryptionResult(string? Secret, bool IsUnavailable, string? Message)
    {
        public static TotpSecretDecryptionResult Decrypted(string secret) => new(secret, false, null);

        public static TotpSecretDecryptionResult Unavailable(string message) => new(null, true, message);
    }
}
