using ST.Application.Contracts.Interfaces.Common;
using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Dashboard;

namespace ST.Application.Features.Shared.WorkTasks;

public sealed record GetTaskAssigneesQuery(DashboardTaskKey Task, string? Search) : IRequest<AppResponse<IReadOnlyList<DashboardTaskAssigneeResponse>>>, ICachableRequest
{
    public bool BypassCache => true;
    public string CacheGroup => "work-tasks";
    public string Discriminator => "live-actor";
    public string? CacheUserId => null;
    public bool IsVersioned => false;
}
