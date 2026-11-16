using MediatR;
using ST.Application.Contracts.Interfaces.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Dashboard;

namespace ST.Application.Features.Shared.Dashboard;

public sealed record GetDashboardAttentionQuery(DashboardAttentionAccess Access)
    : IRequest<AppResponse<IReadOnlyList<DashboardAttentionItemResponse>>>, ICachableRequest
{
    public bool BypassCache => true;
    public string CacheGroup => "dashboard-attention";
    public string Discriminator => "current-actor";
    public string? CacheUserId => null;
    public bool IsVersioned => false;
}
