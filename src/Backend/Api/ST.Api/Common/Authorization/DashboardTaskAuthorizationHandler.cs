using ST.Domain.Features.ControlPlane.Tenants.Entities;
using Microsoft.AspNetCore.Authorization;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.Shared.Dashboard;

namespace ST.Api.Common.Authorization;

internal sealed class DashboardTaskAuthorizationHandler(PermissionAuthorizationHandler permissions)
    : AuthorizationHandler<DashboardTaskPermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, DashboardTaskPermissionRequirement requirement)
    {
        if (context.Resource is not HttpContext http || http.Request.RouteValues["kind"] is not string kind
            || !DashboardTaskCatalog.Definitions.TryGetValue(kind, out var definition))
        {
            return;
        }

        if (context.User.HasClaim(claim => claim.Type is "support_grant_id" or "demo_workspace"))
        {
            return;
        }

        if (definition.IsPlatform && (!PlatformAccess.HasMfa(context.User)
            || !Guid.TryParse(context.User.FindFirst("tenant_id")?.Value, out var owner) || owner != Tenant.PlatformTenantId))
        {
            return;
        }

        foreach (var permission in definition.RequiredPermissions)
        {
            var check = new AuthorizationHandlerContext([new PermissionRequirement(permission)], context.User, context.Resource);
            await permissions.HandleAsync(check).ConfigureAwait(false);

            if (!check.HasSucceeded)
            {
                return;
            }
        }
        context.Succeed(requirement);
    }
}
