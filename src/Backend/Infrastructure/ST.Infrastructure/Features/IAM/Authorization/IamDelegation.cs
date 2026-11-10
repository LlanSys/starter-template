using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ST.Application.Features.ControlPlane.Modules;
using ST.Application.Features.IAM.Users;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Persistence.Features.IAM.DataContext;

namespace ST.Infrastructure.Features.IAM.Authorization;

internal sealed class IamDelegation(
    IamDBContext iam,
    IHttpContextAccessor http,
    IEffectiveModulePolicy modules,
    RoleManager<AppRole> roleManager)
{
    public bool CanDelegate(IEnumerable<string> permissions)
    {
        var actor = http.HttpContext?.User;
        return actor?.Identity?.IsAuthenticated == true
            && !actor.HasClaim(claim => claim.Type == "support_grant_id")
            && permissions.All(key => !key.StartsWith("control_plane.", StringComparison.OrdinalIgnoreCase)
                && HasPermission(actor, key));
    }

    public async Task<bool> CanChangeRolesAsync(
        IEnumerable<string> previous,
        IEnumerable<string> requested,
        CancellationToken cancellationToken)
    {
        var oldRoles = previous.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newRoles = requested.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var changed = oldRoles.Except(newRoles, StringComparer.OrdinalIgnoreCase)
            .Concat(newRoles.Except(oldRoles, StringComparer.OrdinalIgnoreCase)).ToArray();
        if (changed.Length == 0)
        {
            return true;
        }

        if (!CanDelegate(["users.manage_roles"]) || changed.Any(name =>
            string.Equals(name.Trim(), RoleAccessRules.BootstrapRole, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var names = changed.Select(roleManager.NormalizeKey).ToArray();
        var roles = await iam.Roles.AsNoTracking().Where(role => names.Contains(role.NormalizedName))
            .Select(role => new { role.Id, role.ModuleKey }).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (roles.Count != changed.Length)
        {
            return false;
        }

        var access = await modules.GetAsync(cancellationToken).ConfigureAwait(false);
        if (roles.Any(role => role.ModuleKey is not null && !access.Modules.Contains(role.ModuleKey)))
        {
            return false;
        }

        var ids = roles.Select(role => role.Id).ToArray();
        var keys = await iam.RoleClaims.AsNoTracking()
            .Where(claim => ids.Contains(claim.RoleId) && claim.ClaimType == "permission")
            .Select(claim => claim.ClaimValue!).Distinct().ToArrayAsync(cancellationToken).ConfigureAwait(false);
        return keys.All(key => access.PermissionRegistered(key) && access.PermissionAvailable(key)) && CanDelegate(keys);
    }

    private static bool HasPermission(ClaimsPrincipal actor, string permission)
    {
        return actor.HasClaim(claim => string.Equals(claim.Type, "permission", StringComparison.OrdinalIgnoreCase)
            && string.Equals(claim.Value, permission, StringComparison.OrdinalIgnoreCase));
    }
}
