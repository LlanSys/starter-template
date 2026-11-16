using MediatR;
using ST.Application.Contracts.Interfaces.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;

namespace ST.Application.Features.ControlPlane.Tenants.Queries;

public sealed record GetPlatformDashboardQuery(bool IncludeInvitations)
    : IRequest<AppResponse<PlatformDashboardResponse>>, ICachableRequest
{
    public bool BypassCache => true;
    public string CacheGroup => "tenants.dashboard";
    public string Discriminator => IncludeInvitations ? "with-invitations" : "tenant-summary";
    public string? CacheUserId => null;
    public bool IsVersioned => true;
}
