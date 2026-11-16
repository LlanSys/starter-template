using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.Api.Common.Authorization;
using ST.Api.Common.Controllers;
using ST.Application.Features.Shared.WorkTasks;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.Shared.Dashboard;

namespace ST.Api.Features.Shared.Dashboard.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard/tasks/{kind}")]
[RequireTaskPermission]
[RequireModule("core", "/dashboard")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class WorkTasksController(ISender sender, IAuthorizationService authorization) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> List(string kind, CancellationToken cancellationToken,
        string queue = "unassigned", int page = 1, int pageSize = 25)
    {
        return HandleResponse(await sender.Send(new GetDashboardTasksQuery(kind, queue, page, pageSize,
            await CanAssignOthersAsync(kind).ConfigureAwait(false)), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("assignment")]
    public async Task<IActionResult> Assign(string kind, AssignDashboardTaskRequest request, CancellationToken cancellationToken)
    {
        if (request.Task?.Kind != kind)
            return HandleResponse(AppResponses.Failure<bool>(AppError.Validation(new Dictionary<string, string[]> { ["Task.Kind"] = ["Select the matching task category."] })));
        return HandleResponse(await sender.Send(new AssignDashboardTaskCommand(request,
            await CanAssignOthersAsync(kind).ConfigureAwait(false)), cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("assignees")]
    public async Task<IActionResult> Assignees(string kind, Guid subjectId, CancellationToken cancellationToken,
        Guid cycleId = default, DateTimeOffset cycleAt = default, string? search = null)
    {
        if (!await CanAssignOthersAsync(kind).ConfigureAwait(false)) return Forbid();
        return HandleResponse(await sender.Send(new GetTaskAssigneesQuery(
            new DashboardTaskKey(kind, subjectId, cycleId, cycleAt), search), cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("history")]
    public async Task<IActionResult> History(string kind, Guid subjectId, CancellationToken cancellationToken,
        Guid cycleId = default, DateTimeOffset cycleAt = default)
    {
        return HandleResponse(await sender.Send(new GetTaskHistoryQuery(
            new DashboardTaskKey(kind, subjectId, cycleId, cycleAt)), cancellationToken).ConfigureAwait(false));
    }

    private async Task<bool> CanAssignOthersAsync(string kind)
    {
        var permission = DashboardTaskCatalog.Definitions[kind].IsPlatform
            ? PlatformPermissions.ManageTasks : DashboardTaskCatalog.CustomerAssignmentPermission;

        return (await authorization.AuthorizeAsync(User, PermissionAuthorizationConstants.BuildPolicyName(permission)).ConfigureAwait(false)).Succeeded;
    }
}
