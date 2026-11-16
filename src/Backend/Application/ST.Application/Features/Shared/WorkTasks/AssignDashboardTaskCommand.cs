using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Dashboard;

namespace ST.Application.Features.Shared.WorkTasks;

public sealed record AssignDashboardTaskCommand(AssignDashboardTaskRequest Request, bool CanAssignOthers) : IRequest<AppResponse<bool>>;
