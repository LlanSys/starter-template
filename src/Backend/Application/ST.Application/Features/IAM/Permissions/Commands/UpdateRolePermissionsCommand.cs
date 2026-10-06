using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Permissions.Dtos;
using MediatR;

namespace ST.Application.Features.IAM.Permissions.Commands;

public sealed record UpdateRolePermissionsCommand(string RoleId, UpdateRolePermissionsRequest Request, string UserId)
    : IRequest<AppResponse<RolePermissionsResponse>>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> DirectInvalidationKeys => [CacheKeys.Entity("role-permissions", RoleId)];

    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [CacheKeys.GroupVersion("iam-admin")];
}
