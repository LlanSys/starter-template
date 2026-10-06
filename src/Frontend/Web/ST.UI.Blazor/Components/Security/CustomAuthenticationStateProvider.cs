using System.Security.Claims;
using ST.UI.Rcl.Features.IAM.Users.Contracts.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;

namespace ST.UI.Blazor.Components.Security;

/// <summary>
/// Bridges <see cref="IAuthSession"/> into the native Blazor
/// <see cref="AuthenticationStateProvider"/> so that <c>&lt;AuthorizeRouteView&gt;</c>,
/// <c>[Authorize]</c>, and <c>&lt;AuthorizeView&gt;</c> work against the existing
/// session-backed identity without duplicating token handling.
/// </summary>
internal sealed class CustomAuthenticationStateProvider(IAuthSession authSession) : AuthenticationStateProvider
{
    private static readonly AuthenticationState AnonymousState =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!authSession.IsInitialized)
        {
            await authSession.InitializeAsync().ConfigureAwait(false);
        }

        if (!authSession.IsAuthenticated || authSession.CurrentUser is null)
        {
            return AnonymousState;
        }

        var user = authSession.CurrentUser;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.AppUserId),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
        };

        if (!string.IsNullOrWhiteSpace(user.SessionId))
        {
            claims.Add(new Claim("session_id", user.SessionId));
        }

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (user.Permissions is not null)
        {
            foreach (var permission in user.Permissions)
            {
                claims.Add(new Claim("permission", permission));
            }
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "StarterTemplate.Session");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    /// <summary>
    /// Call after sign-in, sign-out, or session refresh to push the updated
    /// identity into the Blazor authorization system.
    /// </summary>
    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
