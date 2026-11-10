using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using ST.Application.Features.ControlPlane.Modules;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Features.ControlPlane;

namespace ST.Api.Middleware;

internal sealed class LiveAuthorityMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IamDBContext iam, IEffectiveModulePolicy modules)
    {
        var ticket = context.Features.Get<IAuthenticateResultFeature>()?.AuthenticateResult?.Ticket;
        if (ticket?.AuthenticationScheme != JwtBearerDefaults.AuthenticationScheme)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var principal = context.User;
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantId = Guid.Parse(principal.FindFirstValue("tenant_id")!);
        var cancellationToken = context.RequestAborted;
        var account = await iam.Users.AsNoTracking()
            .Where(user => user.Id == userId && user.TenantId == tenantId && user.IsActive && !user.IsDeleted)
            .Select(user => new { user.RequiresAdministratorSetup, user.TwoFactorEnabled, user.EmailConfirmed })
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (account is null || !account.EmailConfirmed)
        {
            await Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "This account session is unavailable.")
                .ExecuteAsync(context).ConfigureAwait(false);
            return;
        }

        var claims = principal.Claims.Where(claim => !IsAuthorityClaim(claim.Type)).ToList();
        claims.Add(new Claim("mfa_enrolled", account.TwoFactorEnabled ? "true" : "false"));
        if (!account.RequiresAdministratorSetup)
        {
            var roles = from membership in iam.UserRoles.AsNoTracking()
                        join role in iam.Roles.AsNoTracking() on membership.RoleId equals role.Id
                        where membership.UserId == userId && role.TenantId == tenantId && !role.IsDeleted && role.Name != null
                        select role;

            var access = await modules.GetAsync(cancellationToken).ConfigureAwait(false);
            var enabledModules = access.Modules.ToArray();
            var availableRoles = roles.Where(role => role.ModuleKey == null || enabledModules.Contains(role.ModuleKey));
            var roleNames = await availableRoles.Select(role => role.Name!)
                .ToArrayAsync(cancellationToken).ConfigureAwait(false);
            var rolePermissions = from role in availableRoles
                                  join claim in iam.RoleClaims.AsNoTracking() on role.Id equals claim.RoleId
                                  where claim.ClaimType == "permission" && claim.ClaimValue != null
                                  select claim.ClaimValue!;

            var directClaims = await iam.UserClaims.AsNoTracking()
                .Where(claim => claim.UserId == userId && claim.ClaimValue != null
                    && (claim.ClaimType == "permission" || claim.ClaimType == PlatformAccess.StaffClaim))
                .Select(claim => new { claim.ClaimType, claim.ClaimValue })
                .ToArrayAsync(cancellationToken).ConfigureAwait(false);
            var grantedByRole = await rolePermissions.Distinct().ToArrayAsync(cancellationToken).ConfigureAwait(false);
            var proxy = principal.HasClaim("demo_workspace", "true") || principal.HasClaim(claim => claim.Type == "support_grant_id");
            var permissions = directClaims.Where(claim => claim.ClaimType == "permission").Select(claim => claim.ClaimValue!)
                .Concat(grantedByRole.Where(key => !key.StartsWith("control_plane.", StringComparison.OrdinalIgnoreCase)))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            claims.AddRange(roleNames.Select(name => new Claim(ClaimTypes.Role, name)));
            claims.AddRange(permissions.Where(key => !string.IsNullOrWhiteSpace(key))
                .Where(key => key.StartsWith("control_plane.", StringComparison.OrdinalIgnoreCase)
                    ? !proxy : access.PermissionRegistered(key) && access.PermissionAvailable(key))
                .Select(key => new Claim("permission", key)));

            if (!proxy && directClaims.Any(claim => claim.ClaimType == PlatformAccess.StaffClaim && claim.ClaimValue == "true"))
            {
                claims.Add(new Claim(PlatformAccess.StaffClaim, "true"));
            }
        }

        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, principal.Identity!.AuthenticationType,
            ClaimTypes.Name, ClaimTypes.Role));
        await next(context).ConfigureAwait(false);
    }

    private static bool IsAuthorityClaim(string claimType)
    {
        return string.Equals(claimType, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)
            || string.Equals(claimType, "role", StringComparison.OrdinalIgnoreCase)
            || string.Equals(claimType, "roles", StringComparison.OrdinalIgnoreCase)
            || string.Equals(claimType, "permission", StringComparison.OrdinalIgnoreCase)
            || string.Equals(claimType, PlatformAccess.StaffClaim, StringComparison.OrdinalIgnoreCase)
            || string.Equals(claimType, "mfa_enrolled", StringComparison.OrdinalIgnoreCase);
    }
}
