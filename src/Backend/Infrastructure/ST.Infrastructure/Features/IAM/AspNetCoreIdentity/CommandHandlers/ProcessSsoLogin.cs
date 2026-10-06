using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Features.Shared.Common.Enums;
using ST.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class ProcessSsoLogin(
    UserManager<AppUser> userManager,
    IHttpContextAccessor httpContextAccessor,
    IJwtService jwtService,
    IClaimsService claimsService,
    ISessionService sessionService,
    IDistributedCache cache,
    IIamUnitOfWork iamUnitOfWork,
    ICurrentTenantProvider currentTenantProvider,
    IOptions<JwtSettings> jwtSettings,
    IOptions<EntraIdSettings> entraIdSettings,
    ILogger<ProcessSsoLogin> logger) : IRequestHandler<ProcessSsoLoginCommand, AppResponse<string>>
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;
    private readonly EntraIdSettings _entraIdSettings = entraIdSettings.Value;

    public async Task<AppResponse<string>> Handle(ProcessSsoLoginCommand command, CancellationToken cancellationToken)
    {
        var tenantId = currentTenantProvider.TenantId;

        var user = await userManager.FindByLoginAsync(command.Provider, command.ProviderKey).ConfigureAwait(false);

        if (user == null)
        {
            user = await userManager.FindByEmailAsync(command.Email).ConfigureAwait(false);

            if (user != null)
            {
                var linkResult = await userManager.AddLoginAsync(user, new UserLoginInfo(command.Provider, command.ProviderKey, command.Provider)).ConfigureAwait(false);
                if (!linkResult.Succeeded)
                {
                    ServiceLogDefinitions.LogSsoExternalLoginLinkFailed(logger, command.Email);
                    return AppResponses.Failure<string>("Failed to link external account.");
                }
            }
            else if (_entraIdSettings.AutoLinkByVerifiedEmail)
            {
                user = AppUser.CreateExternalUser(
                    tenantId,
                    command.Email,
                    command.Email,
                    command.FirstName,
                    command.LastName,
                    "System"
                );

                var createResult = await userManager.CreateAsync(user).ConfigureAwait(false);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    ServiceLogDefinitions.LogSsoUserCreationError(logger, command.Email, errors);
                    return AppResponses.Failure<string>("Failed to provision user.");
                }

                await userManager.AddToRoleAsync(user, "User").ConfigureAwait(false);

                await userManager.AddLoginAsync(user, new UserLoginInfo(command.Provider, command.ProviderKey, command.Provider)).ConfigureAwait(false);
            }
            else
            {
                return AppResponses.Failure<string>("User not found and auto-provisioning is disabled.");
            }
        }

        if (!user.IsActive || user.IsDeleted)
        {
            return AppResponses.Failure<string>("This account is inactive. Please contact support.");
        }

        var sessionId = Guid.CreateVersion7();
        var sessionCreationResult = await sessionService.CreateSessionAsync(
            user.Id,
            sessionId,
            httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown",
            httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown",
            "SSO",
            cancellationToken).ConfigureAwait(false);

        if (!sessionCreationResult.IsSuccess)
        {
            return AppResponses.Failure<string>("Could not establish a user session.");
        }

        var activeSessionId = sessionCreationResult.Data;
        var userClaims = await claimsService.GetUserClaimsAsync(user, activeSessionId).ConfigureAwait(false);
        var tokenResponse = await jwtService.CreateTokenAsync(userClaims).ConfigureAwait(false);
        var refreshToken = jwtService.CreateRefreshToken();

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
            await iamUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }).ConfigureAwait(false);

        var rolesResponse = await userManager.GetRolesAsync(user).ConfigureAwait(false);

        var appUserResponse = user.ToAppUserResponse(rolesResponse);

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
                DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
                appUserResponse,
                userClaims.ToClaimResponses(),
                false);

        var exchangeCode = Guid.CreateVersion7().ToString("N");

        await cache.SetStringAsync(
            $"SSO_Exchange_{exchangeCode}",
            JsonSerializer.Serialize(loginResponse),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2) },
            cancellationToken).ConfigureAwait(false);

        return AppResponses.Success("SSO Login successful", exchangeCode);
    }
}
