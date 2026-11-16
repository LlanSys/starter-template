using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Dashboard;

namespace ST.Application.Features.Shared.WorkTasks;

internal sealed class WorkTaskHandler(IWorkTaskService service) :
    IRequestHandler<GetDashboardTasksQuery, AppResponse<DashboardTaskPageResponse>>,
    IRequestHandler<AssignDashboardTaskCommand, AppResponse<bool>>,
    IRequestHandler<GetTaskAssigneesQuery, AppResponse<IReadOnlyList<DashboardTaskAssigneeResponse>>>,
    IRequestHandler<GetTaskHistoryQuery, AppResponse<IReadOnlyList<DashboardTaskHistoryResponse>>>
{
    public Task<AppResponse<DashboardTaskPageResponse>> Handle(GetDashboardTasksQuery request, CancellationToken cancellationToken)
    {
        return service.ListAsync(request.Kind, request.Queue, request.Page, request.PageSize, request.CanAssignOthers, cancellationToken);
    }

    public Task<AppResponse<bool>> Handle(AssignDashboardTaskCommand request, CancellationToken cancellationToken)
    {
        return service.AssignAsync(request.Request, request.CanAssignOthers, cancellationToken);
    }

    public Task<AppResponse<IReadOnlyList<DashboardTaskAssigneeResponse>>> Handle(GetTaskAssigneesQuery request, CancellationToken cancellationToken)
    {
        return service.AssigneesAsync(request.Task, request.Search, cancellationToken);
    }

    public Task<AppResponse<IReadOnlyList<DashboardTaskHistoryResponse>>> Handle(GetTaskHistoryQuery request, CancellationToken cancellationToken)
    {
        return service.HistoryAsync(request.Task, cancellationToken);
    }
}
