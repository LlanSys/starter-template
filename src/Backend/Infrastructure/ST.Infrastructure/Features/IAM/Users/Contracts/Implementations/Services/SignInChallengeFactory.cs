using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Security;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Extensions;
using ST.SharedKernel.Features.IAM.Users.Dtos;

namespace ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Services;

internal sealed class SignInChallengeFactory(IJwtService jwt, TimeProvider timeProvider)
{
    public async Task<AppResponse<LoginResponse>> CreateAsync(AppUser user)
    {
        var lifetime = TimeSpan.FromMinutes(5);
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id),
            new("tenant_id", user.TenantId.ToString()),
            new("temp_auth", "true"),
            new("credential_stamp", SingleUseCredential.Hash(user.SecurityStamp ?? string.Empty))
        ];

        var token = await jwt.CreateTempTokenAsync(claims, lifetime).ConfigureAwait(false);
        return AppResponses.Success("Two-factor authentication required.", new LoginResponse(
            user.Id, user.FirstName, user.LastName, user.Email ?? string.Empty, true, false, false,
            token, string.Empty, null, timeProvider.GetUtcNow().Add(lifetime), user.ToAppUserResponse(), null));
    }

    public ClaimsPrincipal? Validate(AppUser user, string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 4096)
        {
            return null;
        }

        try
        {
            var principal = jwt.GetPrincipalFromToken(token, validateLifetime: true);
            return principal?.Identity?.IsAuthenticated == true
                && principal.FindFirstValue(ClaimTypes.NameIdentifier) == user.Id
                && principal.FindFirstValue("tenant_id") == user.TenantId.ToString()
                ? principal : null;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }

    public static bool IsFirstFactorChallenge(ClaimsPrincipal principal, AppUser user) =>
        principal.HasClaim("temp_auth", "true")
        && principal.HasClaim("credential_stamp", SingleUseCredential.Hash(user.SecurityStamp ?? string.Empty));
}
