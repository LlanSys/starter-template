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
    [RequirePermission("control_plane.manage")]
    [ProducesResponseType(typeof(AppResponse<ImpersonationRecordResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> StartImpersonation(
        [FromBody] StartImpersonationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new StartImpersonationCommand(request.TargetTenantId, request.Reason, request.DurationHours);
        return HandleResponse(await sender.Send(command, cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("impersonate/end")]
    [RequirePermission("control_plane.manage")]
    [ProducesResponseType(typeof(AppResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EndImpersonation(
        [FromBody] EndImpersonationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new EndImpersonationCommand(request.ImpersonationRecordId);
        return HandleResponse(await sender.Send(command, cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("impersonate/records")]
    [RequirePermission("control_plane.manage")]
    [ProducesResponseType(typeof(AppResponse<IReadOnlyList<ImpersonationRecordDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetImpersonationRecords(
        [FromQuery] bool activeOnly,
        CancellationToken cancellationToken)
    {
        return HandleResponse(await sender.Send(new GetImpersonationRecordsQuery(activeOnly), cancellationToken).ConfigureAwait(false));
    }
}
