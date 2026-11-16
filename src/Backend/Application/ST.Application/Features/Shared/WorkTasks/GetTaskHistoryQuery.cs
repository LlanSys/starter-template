using ST.Application.Contracts.Interfaces.Common;
using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Dashboard;

namespace ST.Application.Features.Shared.WorkTasks;

public sealed record GetTaskHistoryQuery(DashboardTaskKey Task) : IRequest<AppResponse<IReadOnlyList<DashboardTaskHistoryResponse>>>, ICachableRequest
{
    public bool BypassCache => true;
    public string CacheGroup => "work-tasks";
    public string Discriminator => "live-actor";
    public string? CacheUserId => null;
    public bool IsVersioned => false;
}
