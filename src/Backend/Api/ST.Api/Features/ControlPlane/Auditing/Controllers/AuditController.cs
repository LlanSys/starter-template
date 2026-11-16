using ST.SharedKernel.Features.ControlPlane;
using Asp.Versioning;
using ST.Api.Common.Authorization;
using ST.Api.Common.Controllers;
using ST.Application.Features.ControlPlane.Auditing.Commands;
using ST.Application.Features.ControlPlane.Auditing.Queries;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Auditing.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ST.Api.Features.ControlPlane.Auditing.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/control-plane/audit")]
[ApiController]
[Authorize]
public class AuditController(ISender sender) : BaseController
{
    [HttpPost("impersonate/start")]
    [ProducesResponseType(typeof(AppResponse<ImpersonationRecordResponse>), StatusCodes.Status200OK)]
    [RequirePermission(PlatformPermissions.ManageSupport)]
    public async Task<IActionResult> StartImpersonation(
        [FromBody] StartImpersonationRequest request,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        return Conflict(AppResponses.Failure<ImpersonationRecordResponse>("Managed tenant support is not yet available."));
    }

    [HttpPost("impersonate/end")]
    [ProducesResponseType(typeof(AppResponse<bool>), StatusCodes.Status200OK)]
    [RequirePermission(PlatformPermissions.ManageSupport)]
    public async Task<IActionResult> EndImpersonation(
        [FromBody] EndImpersonationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new EndImpersonationCommand(request.ImpersonationRecordId);
        return HandleResponse(await sender.Send(command, cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("impersonate/records")]
    [ProducesResponseType(typeof(AppResponse<IReadOnlyList<ImpersonationRecordDto>>), StatusCodes.Status200OK)]
    [RequirePermission(PlatformPermissions.ViewAudit)]
    public async Task<IActionResult> GetImpersonationRecords(
        [FromQuery] bool activeOnly,
        CancellationToken cancellationToken)
    {
        return HandleResponse(await sender.Send(new GetImpersonationRecordsQuery(activeOnly), cancellationToken).ConfigureAwait(false));
    }
}
