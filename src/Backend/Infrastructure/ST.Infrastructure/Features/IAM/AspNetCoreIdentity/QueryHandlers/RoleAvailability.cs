using Microsoft.EntityFrameworkCore;
using ST.Application.Features.ControlPlane.Modules;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Persistence.Features.IAM.DataContext;
namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.QueryHandlers;
internal sealed class RoleAvailability(IamDBContext context, IEffectiveModulePolicy policy) : IRoleAvailability
{
    public async Task<IReadOnlySet<string>> AvailableIdsAsync(CancellationToken cancellationToken = default)
    {
        var access = await policy.GetAsync(cancellationToken).ConfigureAwait(false);
        var roles = await context.Roles.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var ids = roles.Select(x => x.Id).ToArray();
        var claims = await context.RoleClaims.AsNoTracking().Where(x => ids.Contains(x.RoleId) && x.ClaimType == "permission").ToListAsync(cancellationToken).ConfigureAwait(false);
        var lookup = claims.ToLookup(x => x.RoleId);
        return roles.Where(role => (role.ModuleKey is null || access.Modules.Contains(role.ModuleKey)) && (!lookup[role.Id].Any() || lookup[role.Id].Any(claim => access.PermissionAvailable(claim.ClaimValue ?? "")))).Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
    }
}
