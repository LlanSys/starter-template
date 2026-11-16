using Microsoft.EntityFrameworkCore;
using ST.Application.Features.ControlPlane.Modules;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.Shared.Dashboard;

namespace ST.Infrastructure.Features.Shared.WorkTasks;

internal sealed class TaskAssigneeEligibility(IamDBContext iam, ICurrentTenantProvider tenant, IEffectiveModulePolicy modules)
{
    public async Task<IReadOnlyList<DashboardTaskAssigneeResponse>> FindAsync(DashboardTaskDefinition definition,
        string[]? userIds, string? search, CancellationToken cancellationToken)
    {
        var owner = tenant.TenantId;
        var query = iam.Users.IgnoreQueryFilters().AsNoTracking().Where(user => user.TenantId == owner
            && user.IsActive && !user.IsDeleted && user.EmailConfirmed && user.TwoFactorEnabled && (user.AdministratorInvitationId == null || user.AdministratorSetupCompletedAt != null)
            && !iam.UserClaims.Any(claim => claim.UserId == user.Id && (claim.ClaimType == "support_grant_id"
                || (claim.ClaimType == "demo_workspace" && claim.ClaimValue == "true"))));

        if (userIds is not null) query = query.Where(user => userIds.Contains(user.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(user => (user.NormalizedUserName != null && user.NormalizedUserName.Contains(term))
                || (user.NormalizedEmail != null && user.NormalizedEmail.Contains(term)));
        }

        if (definition.IsPlatform)
        {
            if (owner != Tenant.PlatformTenantId) return [];
            query = query.Where(user => iam.UserClaims.Any(claim => claim.UserId == user.Id
                && claim.ClaimType == PlatformAccess.StaffClaim && claim.ClaimValue == "true"));

            foreach (var permission in definition.RequiredPermissions)
            {
                query = query.Where(user => iam.UserClaims.Any(claim => claim.UserId == user.Id
                    && claim.ClaimType == "permission" && claim.ClaimValue == permission));
            }
        }
        else
        {
            var access = await modules.GetAsync(cancellationToken).ConfigureAwait(false);

            if (definition.RequiredPermissions.Any(key => !access.PermissionRegistered(key) || !access.PermissionAvailable(key))) return [];
            var enabled = access.Modules.ToArray();

            foreach (var permission in definition.RequiredPermissions)
            {
                query = query.Where(user => iam.UserClaims.Any(claim => claim.UserId == user.Id
                    && claim.ClaimType == "permission" && claim.ClaimValue == permission)
                    || (from membership in iam.UserRoles
                        join role in iam.Roles.IgnoreQueryFilters() on membership.RoleId equals role.Id
                        join claim in iam.RoleClaims on role.Id equals claim.RoleId
                        where membership.UserId == user.Id && role.TenantId == owner && !role.IsDeleted && role.Name != null
                            && (role.ModuleKey == null || enabled.Contains(role.ModuleKey))
                            && claim.ClaimType == "permission" && claim.ClaimValue == permission
                        select claim.Id).Any());
            }
        }
        var accounts = query.OrderBy(user => user.UserName).ThenBy(user => user.Id);
        return await (userIds is null ? accounts.Take(50) : accounts)
            .Select(user => new DashboardTaskAssigneeResponse(user.Id,
                user.FirstName + " " + user.LastName + " (" + (user.UserName ?? "Account") + ")"))
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);
    }
}
