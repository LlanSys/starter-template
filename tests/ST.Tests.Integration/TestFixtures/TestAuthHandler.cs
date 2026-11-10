using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace ST.Tests.Integration.TestFixtures;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "TestScheme";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.Authorization.ToString().StartsWith(AuthenticationScheme, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim>();

        if (Request.Headers.TryGetValue("X-Test-UserId", out var userId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
            claims.Add(new Claim("sub", userId.ToString()));
        }
        else
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, "test-user-id"));
            claims.Add(new Claim("sub", "test-user-id"));
        }

        if (Request.Headers.TryGetValue("X-Test-TenantId", out var tenantId))
        {
            claims.Add(new Claim("tenant_id", tenantId.ToString()));
        }

        if (Request.Headers.TryGetValue("X-Test-Permissions", out var permissions))
        {
            var perms = permissions.ToString().Split(',');
            foreach (var perm in perms)
            {
                claims.Add(new Claim("permission", perm.Trim()));
            }
        }

        if (Request.Headers["X-Test-PlatformStaff"] == "true") claims.Add(new Claim("platform_staff", "true"));

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
