using ST.Application.Contracts.Interfaces.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;

namespace ST.Application.Features.IAM.Users.Queries;

public sealed record GetAdminUserDevicesQuery : IRequest<AppResponse<IReadOnlyList<AdminUserDeviceResponse>>>, ICachableRequest
{
    public string CacheGroup => "iam-admin";
    public string Discriminator => "user-devices";
    public string? CacheUserId => null;
    public bool IsVersioned => true;
    public bool BypassCache => true;
}
