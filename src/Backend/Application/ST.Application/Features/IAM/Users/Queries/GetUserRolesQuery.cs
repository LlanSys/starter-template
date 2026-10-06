using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;

namespace ST.Application.Features.IAM.Users.Queries;

public sealed record GetUserRolesQuery(string UserId, string RequestedBy)
    : IRequest<AppResponse<UserRolesResponse>>, ICachableRequest
{
    public string CacheGroup => "iam-admin";
    public string Discriminator => CacheKeys.Entity("user-roles", UserId);
    public string? CacheUserId => null;
    public bool IsVersioned => true;
    public bool BypassCache => true;
}
