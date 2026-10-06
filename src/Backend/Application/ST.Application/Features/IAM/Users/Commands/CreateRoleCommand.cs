using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;

namespace ST.Application.Features.IAM.Users.Commands;

public sealed record CreateRoleCommand(CreateRoleRequest Request, string CreatedBy)
    : IRequest<AppResponse<AdminRoleListResponse>>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> DirectInvalidationKeys => [];

    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [CacheKeys.GroupVersion("iam-admin")];
}
