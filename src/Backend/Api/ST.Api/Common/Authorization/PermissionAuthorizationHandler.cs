using Microsoft.AspNetCore.Authorization;

namespace ST.Api.Common.Authorization;

internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (context.User.IsInRole(PermissionAuthorizationConstants.SystemAdministratorRole)
            || context.User.Claims.Any(claim =>
                string.Equals(claim.Type, PermissionAuthorizationConstants.ClaimType, StringComparison.OrdinalIgnoreCase)
                && string.Equals(claim.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
