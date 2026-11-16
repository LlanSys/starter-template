using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Dashboard;

namespace ST.Application.Features.Shared.WorkTasks;

public interface IWorkTaskService
{
    Task<DashboardTaskCounts> CountsAsync(string kind, CancellationToken cancellationToken);
    Task<AppResponse<DashboardTaskPageResponse>> ListAsync(string kind, string queue, int page, int pageSize, bool canAssignOthers, CancellationToken cancellationToken);
    Task<AppResponse<bool>> AssignAsync(AssignDashboardTaskRequest request, bool canAssignOthers, CancellationToken cancellationToken);
    Task<AppResponse<IReadOnlyList<DashboardTaskAssigneeResponse>>> AssigneesAsync(DashboardTaskKey key, string? search, CancellationToken cancellationToken);
    Task<AppResponse<IReadOnlyList<DashboardTaskHistoryResponse>>> HistoryAsync(DashboardTaskKey key, CancellationToken cancellationToken);
    Task<Guid?> ReconcileAsync(string kind, Guid? after, CancellationToken cancellationToken);
}
