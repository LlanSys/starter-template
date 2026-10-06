
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Features.Shared.Common.Enums;
using ST.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.IdentityModel.Tokens.Jwt;

using Microsoft.Extensions.Caching.Distributed;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class LoginWithPasskey(
    UserManager<AppUser> userManager,
    IHttpContextAccessor httpContextAccessor,
    IJwtService jwtService,
    IClaimsService claimsService,
    IServiceManager serviceManager,
    IIamUnitOfWork iamUnitOfWork,
    IPasskeyService passkeyService,
    IDistributedCache cacheService,
    TimeProvider timeProvider,
    IOptions<JwtSettings> jwtSettings,
    ILogger<LoginWithPasskey> logger) : IRequestHandler<LoginWithPasskeyCommand, AppResponse<LoginResponse>>
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public async Task<AppResponse<LoginResponse>> Handle(LoginWithPasskeyCommand command, CancellationToken cancellationToken)
    {
        var cacheKey = $"Fido2AssertionOptions:{command.CorrelationId}";
        var originalOptionsStr = await cacheService.GetStringAsync(cacheKey, cancellationToken);
        JsonElement originalOptions = default;

        if (string.IsNullOrEmpty(originalOptionsStr))
        {
            return AppResponses.Failure<LoginResponse>("Login options have expired or do not exist.");
        }
        else
        {
            originalOptions = JsonSerializer.Deserialize<JsonElement>(originalOptionsStr);
        }

        var assertionIdStr = command.AssertionResponse.GetProperty("id").GetString();
        if (string.IsNullOrEmpty(assertionIdStr))
        {
            return AppResponses.Failure<LoginResponse>("Invalid assertion response.");
        }

        var credentialIdBytes = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(assertionIdStr);

        AppUser? user = null;
        ST.Domain.Features.IAM.Users.Entities.Fido2Credential? credential = null;

        if (!string.IsNullOrWhiteSpace(command.Username))
        {
            var normalizedUsername = command.Username.ToUpperInvariant();
            user = await userManager.Users
                .Include(u => u.Fido2Credentials)
                .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUsername || u.NormalizedEmail == normalizedUsername, cancellationToken)
                .ConfigureAwait(false);

            credential = user?.Fido2Credentials.FirstOrDefault(c => c.CredentialId.SequenceEqual(credentialIdBytes));
        }
        else
        {
            // Discoverable credential path: find the credential first
            var untrackedCred = await iamUnitOfWork.Fido2CredentialRepository
                .FindByCondition(c => c.CredentialId == credentialIdBytes)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (untrackedCred != null)
            {
                user = await userManager.Users
                    .Include(u => u.Fido2Credentials)
                    .FirstOrDefaultAsync(u => u.Id == untrackedCred.UserId, cancellationToken)
                    .ConfigureAwait(false);

                // Use the tracked credential so EF can detect changes to SignatureCounter
                credential = user?.Fido2Credentials.FirstOrDefault(c => c.CredentialId.SequenceEqual(credentialIdBytes));
            }
        }

        if (user == null || !user.IsActive || user.IsDeleted || credential == null)
        {
            return AppResponses.Failure<LoginResponse>("Invalid login attempt.");
        }

        try
        {
            var newSignCount = await passkeyService.MakeAssertionAsync(user, command.AssertionResponse, originalOptions, credential.PublicKey, credential.SignatureCounter, cancellationToken);
            if (newSignCount == null)
            {
                return AppResponses.Failure<LoginResponse>("Passkey assertion failed.");
            }

            // Update the signature counter
            credential.SignatureCounter = newSignCount.Value;
            await iamUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogPasskeyAssertionException(logger, user.UserName, ex);
            return AppResponses.Failure<LoginResponse>("Passkey validation failed.");
        }

        await cacheService.RemoveAsync(cacheKey, cancellationToken);

        // Generate session, claims, tokens
        var sessionId = Guid.CreateVersion7();
        var sessionCreationResult = await serviceManager.SessionService.CreateSessionAsync(
            user.Id,
            sessionId,
            httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown",
            httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown",
            "Passkey",
            cancellationToken).ConfigureAwait(false);

        if (!sessionCreationResult.IsSuccess)
        {
            return AppResponses.Failure<LoginResponse>("Could not establish a user session.");
        }

        var activeSessionId = sessionCreationResult.Data;
        var userClaims = await claimsService.GetUserClaimsAsync(user, activeSessionId).ConfigureAwait(false);
        var tokenResponse = await jwtService.CreateTokenAsync(userClaims).ConfigureAwait(false);
        var refreshToken = jwtService.CreateRefreshToken();

        var refreshTokenEntity = ST.Domain.Features.IAM.Users.Entities.RefreshToken.Create(
            user.Id,
            refreshToken,
            timeProvider.GetUtcNow().AddHours(_jwtSettings.RefreshTokenExpiryHours),
            user.Id,
            httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString());

        user.RecordSuccessfulLogin();
        await userManager.UpdateAsync(user).ConfigureAwait(false);

        await iamUnitOfWork.ExecuteInTransactionWithRetryAsync(async () =>
        {
            await iamUnitOfWork.TokenRepository.AddRefreshTokenAsync(refreshTokenEntity).ConfigureAwait(false);
            await iamUnitOfWork.TokenRepository.CleanupExpiredTokensAsync(user.Id).ConfigureAwait(false);
            await iamUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }).ConfigureAwait(false);

        var rolesResponse = await userManager.GetRolesAsync(user).ConfigureAwait(false);

        var appUserResponse = new AppUserResponse(
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
                true,
                false, // twoFactorEnabled = false for now since Passkey can be MFA itself
                user.RequirePasswordChange,
                user.CreatedAt,
                user.LastLoginAt,
                [..rolesResponse],
                user.TenantId,
                user.EmployeeId,
                user.CustomerId);

        var tokenExpiry = timeProvider.GetUtcNow().AddMinutes(_jwtSettings.AccessTokenExpiryMinutes);

        var loginResponse = new LoginResponse(
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
                appUserResponse,
                userClaims.ToClaimResponses(),
                false);

        return AppResponses.Success("Passkey Login successful", loginResponse);
    }
}
