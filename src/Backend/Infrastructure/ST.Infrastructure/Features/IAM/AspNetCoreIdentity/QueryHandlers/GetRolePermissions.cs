using ST.Application.Features.IAM.Permissions.Queries;
using ST.Domain.Features.IAM.Users.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Permissions.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.QueryHandlers;

internal sealed class GetRolePermissions(RoleManager<AppRole> roleManager, ST.Application.Features.ControlPlane.Modules.IEffectiveModulePolicy policy)
    : IRequestHandler<GetRolePermissionsQuery, AppResponse<RolePermissionsResponse>>
{
    public async Task<AppResponse<RolePermissionsResponse>> Handle(GetRolePermissionsQuery request, CancellationToken cancellationToken)
    {
        var role = await roleManager.FindByIdAsync(request.RoleId).ConfigureAwait(false);
        if (role is null)
        {
            return AppResponses.Failure<RolePermissionsResponse>("Role not found.");
        }

        var access = await policy.GetAsync(cancellationToken).ConfigureAwait(false);
        var permissionKeys = (await roleManager.GetClaimsAsync(role).ConfigureAwait(false))
            .Where(static claim => claim.Type == "permission")
            .Select(static claim => claim.Value)
            .Where(access.PermissionAvailable)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return AppResponses.Success(
            "Role permissions loaded.",
            new RolePermissionsResponse(role.Id, role.Name ?? string.Empty, permissionKeys));
    }
}
