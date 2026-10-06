using ST.Domain.Features.IAM.Permissions.Entities;
using ST.SharedKernel.Features.IAM.Permissions.Dtos;

namespace ST.Application.Features.IAM.Permissions.Mappings;

public static class PermissionMapping
{
    public static PermissionResponse ToPermissionResponse(this Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        return new PermissionResponse(
            permission.Id,

            permission.Key,
            permission.Context,
            permission.Resource,
            permission.Action,
            permission.Description,
            permission.IsActive);
    }
}
