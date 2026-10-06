using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Permissions.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Permissions.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.Permissions.Queries;


public sealed record GetPermissionByIdQuery(Guid Id, string UserId) : IRequest<AppResponse<PermissionResponse>>, ICachableRequest
{
    public string CacheGroup => "permissions";

    public string Discriminator => CacheKeys.Entity("permissions", Id.ToString());

    public string? CacheUserId => null;

    public bool IsVersioned => true;
}
