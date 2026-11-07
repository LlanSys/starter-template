using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Services;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Extensions;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using StoredRefreshToken = ST.Domain.Features.IAM.Users.Entities.RefreshToken;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class ExchangeSsoCode(
    SsoCodeStore codes, SignInChallengeFactory challenges, IControlPlaneUnitOfWork plane, ICurrentTenantProvider scope,
    IJwtService jwt, IClaimsService claims, ISessionService sessions, IIamUnitOfWork iam, UserManager<AppUser> users,
    IHttpContextAccessor http, IOptions<JwtSettings> settings, IOptions<MfaSettings> mfaSettings, TimeProvider timeProvider)
    : IRequestHandler<ExchangeSsoCodeCommand, AppResponse<LoginResponse>>
{
    public async Task<AppResponse<LoginResponse>> Handle(ExchangeSsoCodeCommand command, CancellationToken cancellationToken)
    {
        var tenantId = scope.TenantId;
        var active = await plane.Tenants.AnyAsync(tenant => tenant.Id == tenantId && tenant.Status == TenantStatus.Active && !tenant.IsDemo,
            cancellationToken).ConfigureAwait(false);
        var user = active ? await codes.ConsumeAsync(command.Code, null, cancellationToken).ConfigureAwait(false) : null;
        if (user is null || await users.IsLockedOutAsync(user).ConfigureAwait(false))
        {
            return AppResponses.Failure<LoginResponse>("Invalid or expired sign-in code.");
        }

        if (user.TwoFactorEnabled)
        {
            return await challenges.CreateAsync(user).ConfigureAwait(false);
        }

        var ipAddress = http.HttpContext?.Connection.RemoteIpAddress?.ToString();
        var session = await sessions.CreateSessionAsync(user.Id, Guid.CreateVersion7(), ipAddress ?? "unknown",
            http.HttpContext?.Request.Headers.UserAgent.ToString() ?? "unknown", "External sign-in", cancellationToken).ConfigureAwait(false);
        if (!session.IsSuccess || session.Data == Guid.Empty)
        {
            return AppResponses.Failure<LoginResponse>("Could not establish a user session.");
        }

        var userClaims = await claims.GetUserClaimsAsync(user, session.Data).ConfigureAwait(false);
        var token = await jwt.CreateTokenAsync(userClaims).ConfigureAwait(false);
        var refresh = jwt.CreateRefreshToken();
        var now = timeProvider.GetUtcNow();
        var storedRefresh = StoredRefreshToken.Create(now, user.Id, refresh, now.AddHours(settings.Value.RefreshTokenExpiryHours), user.Id, ipAddress);
        await iam.TokenRepository.AddRefreshTokenAsync(storedRefresh).ConfigureAwait(false);
        await iam.CompleteAsync(cancellationToken).ConfigureAwait(false);

        var roles = await users.GetRolesAsync(user).ConfigureAwait(false);
        var enroll = user.RequiresAdministratorSetup
            || roles.Contains("Tenant Administrator", StringComparer.OrdinalIgnoreCase)
            || (mfaSettings.Value.Enabled && mfaSettings.Value.EnforceEnrollment
                && roles.Any(role => mfaSettings.Value.RequiredRoles.Contains(role, StringComparer.OrdinalIgnoreCase)));
        return AppResponses.Success(new LoginResponse(user.Id, user.FirstName, user.LastName, user.Email ?? string.Empty,
            false, false, true, token, refresh, session.Data.ToString(), jwt.GetTokenExpiry(token),
            user.ToAppUserResponse(roles), userClaims.ToClaimResponses(), enroll));
    }
}
