using Microsoft.AspNetCore.Authorization;

namespace ST.Api.Common.Authorization;

internal sealed class PermissionAuthorizationHandler(ST.Application.Features.ControlPlane.Modules.IEffectiveModulePolicy policy) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (requirement.Permission.StartsWith("control_plane.", StringComparison.OrdinalIgnoreCase))
        {
            if (ST.SharedKernel.Features.ControlPlane.PlatformAccess.IsStaff(context.User)) context.Succeed(requirement);
            return;
        }

        var access = await policy.GetAsync().ConfigureAwait(false);
        if (!access.PermissionAvailable(requirement.Permission)) return;

        if (context.User.IsInRole(PermissionAuthorizationConstants.SystemAdministratorRole)
            || context.User.Claims.Any(claim =>
                string.Equals(claim.Type, PermissionAuthorizationConstants.ClaimType, StringComparison.OrdinalIgnoreCase)
                && string.Equals(claim.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return;
    }
}
