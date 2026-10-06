using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.SharedKernel.Extensions;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.Mappings;
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
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class VerifyEmailOtp(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IClaimsService claimsService,
    IJwtService jwtService,
    IDistributedCache cache,
    IIamUnitOfWork iamUnitOfWork,
    ISessionService sessionService,
    IHttpContextAccessor httpContextAccessor,
    IOptions<JwtSettings> jwtSettings,
    ILogger<VerifyEmailOtp> logger) : IRequestHandler<VerifyEmailOtpCommand, AppResponse<VerifyEmailOtpResponse>>
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public async Task<AppResponse<VerifyEmailOtpResponse>> Handle(VerifyEmailOtpCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var user = await userManager.FindByIdAsync(req.UserId).ConfigureAwait(false);
        if (user == null) return AppResponses.Failure<VerifyEmailOtpResponse>("User not found");

        var attemptKey = CacheKeys.EmailOtpAttempts(user.Id);
        var attemptsStr = await cache.GetStringAsync(attemptKey, ct).ConfigureAwait(false);
        var attempts = int.TryParse(attemptsStr, out var a) ? a : 0;
        if (attempts >= 3)
            return AppResponses.Failure<VerifyEmailOtpResponse>("Too many attempts. Request a new code.");

        var otpKey = CacheKeys.EmailOtp(user.Id);
        var storedHash = await cache.GetStringAsync(otpKey, ct).ConfigureAwait(false);
        if (storedHash == null)
            return AppResponses.Failure<VerifyEmailOtpResponse>("Code expired. Request a new code.");

        var providedHash = HashCode(user.Id, req.Code, req.Purpose);
        var isValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(storedHash),
            Encoding.UTF8.GetBytes(providedHash));

        if (!isValid)
        {
            await cache.SetStringAsync(attemptKey, (attempts + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) }, ct).ConfigureAwait(false);
            ServiceLogDefinitions.LogInvalidEmailOtp(logger, user.Id);
            return AppResponses.Failure<VerifyEmailOtpResponse>("Invalid code");
        }

        await cache.RemoveAsync(otpKey, ct).ConfigureAwait(false);
        await cache.RemoveAsync(attemptKey, ct).ConfigureAwait(false);

        if (string.Equals(req.Purpose, "EmailConfirmation", StringComparison.OrdinalIgnoreCase))
        {
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await userManager.UpdateAsync(user).ConfigureAwait(false);
                ServiceLogDefinitions.LogEmailConfirmedViaOtp(logger, user.Id);
            }
            return AppResponses.Success("Email confirmed",
                new VerifyEmailOtpResponse(string.Empty, string.Empty, user.Id, null, true, DateTimeOffset.UtcNow, null!, []));
        }

        if (string.Equals(req.Purpose, "PasswordReset", StringComparison.OrdinalIgnoreCase))
        {
            return AppResponses.Success("Code verified",
                new VerifyEmailOtpResponse(string.Empty, string.Empty, user.Id, null, true, DateTimeOffset.UtcNow, null!, []));
        }

        if (!string.Equals(req.Purpose, "Login", StringComparison.OrdinalIgnoreCase))
        {
            return AppResponses.Success("Code verified",
                new VerifyEmailOtpResponse(string.Empty, string.Empty, user.Id, null, true, DateTimeOffset.UtcNow, null!, []));
        }

        var requestedSessionId = Guid.CreateVersion7();
        var sessionCreation = await sessionService.CreateSessionAsync(
            user.Id,
            requestedSessionId,
            httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown",
            httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown",
            string.IsNullOrWhiteSpace(req.DeviceFingerprint) ? "unknown" : req.DeviceFingerprint,
            ct).ConfigureAwait(false);

        if (!sessionCreation.IsSuccess || sessionCreation.Data == Guid.Empty)
        {
            ServiceLogDefinitions.LogFailedToCreateUserSession(logger, user.Id);
            return AppResponses.Failure<VerifyEmailOtpResponse>("Could not establish a user session.");
        }

        var activeSessionId = sessionCreation.Data;
        var claims = await claimsService.GetUserClaimsAsync(user, activeSessionId).ConfigureAwait(false);
        var token = await jwtService.CreateTokenAsync(claims).ConfigureAwait(false);
        var refreshToken = jwtService.CreateRefreshToken();
        var expiry = jwtService.GetTokenExpiry(token);
        var refreshTokenEntity = ST.Domain.Features.IAM.Users.Entities.RefreshToken.Create(
            user.Id,
            refreshToken,
            DateTimeOffset.UtcNow.AddHours(_jwtSettings.RefreshTokenExpiryHours),
            user.Id,
            httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString());

        user.RecordSuccessfulLogin();
        await userManager.UpdateAsync(user).ConfigureAwait(false);
        await iamUnitOfWork.ExecuteInTransactionWithRetryAsync(async () =>
        {
            await iamUnitOfWork.TokenRepository.AddRefreshTokenAsync(refreshTokenEntity).ConfigureAwait(false);
            await iamUnitOfWork.TokenRepository.CleanupExpiredTokensAsync(user.Id).ConfigureAwait(false);
            await iamUnitOfWork.CompleteAsync(ct).ConfigureAwait(false);
            return true;
        }).ConfigureAwait(false);

        if (req.RememberDevice) await signInManager.RememberTwoFactorClientAsync(user).ConfigureAwait(false);
        await signInManager.SignInAsync(user, req.RememberMe).ConfigureAwait(false);

        var roles = await userManager.GetRolesAsync(user).ConfigureAwait(false);
        var twoFactor = await userManager.GetTwoFactorEnabledAsync(user).ConfigureAwait(false);

        var appUser = new AppUserResponse(
            user.Id, user.UserName ?? "", user.FirstName, user.LastName,
            $"{user.FirstName} {user.LastName}".Trim(), user.PhoneNumber, user.NationalId,
            user.Email ?? "", user.Gender.ToDisplayString(), ProfilePictureUrlMapping.ToCurrentUserRoute(user.ProfilePictureUrl),
            user.IsActive, twoFactor, user.RequirePasswordChange, user.CreatedAt,
            user.LastLoginAt, [.. roles], user.TenantId, user.EmployeeId, user.CustomerId);

        var claimsResp = claims.Select(c => new UserClaimsResponse
        {
            Type = c.Type,
            Value = c.Value,
            ValueType = c.ValueType,
            Issuer = c.Issuer,
            OriginalIssuer = c.OriginalIssuer,
            Properties = c.Properties.ToDictionary(k => k.Key, v => v.Value)

        }).ToList();

        return AppResponses.Success("Email OTP verified", new VerifyEmailOtpResponse(
            token,
            refreshToken ?? "",
            user.Id,
            activeSessionId.ToString(),
            true,
            expiry,
            appUser,
            claimsResp));
    }

    private static string HashCode(string userId, string code, string purpose)
    {
        var input = $"{userId}:{purpose}:{code}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash);
    }
}
