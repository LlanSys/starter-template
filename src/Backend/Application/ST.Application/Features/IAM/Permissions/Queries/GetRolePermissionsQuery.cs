using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Permissions.Dtos;
using MediatR;

namespace ST.Application.Features.IAM.Permissions.Queries;

public sealed record GetRolePermissionsQuery(string RoleId, string UserId)
    : IRequest<AppResponse<RolePermissionsResponse>>, ICachableRequest
{
    public string CacheGroup => "iam-admin";

    public string Discriminator => CacheKeys.Entity("role-permissions", RoleId);

    public string? CacheUserId => null;

    public bool IsVersioned => true;

    public bool BypassCache => true;
}
