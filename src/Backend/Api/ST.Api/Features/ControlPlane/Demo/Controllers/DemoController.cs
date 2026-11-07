using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.Api.Common.Authorization;
using ST.Api.Common.Controllers;
using ST.Application.Features.ControlPlane.Demo;
using ST.Domain.Shared.Contracts.Common;
namespace ST.Api.Features.ControlPlane.Demo.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, RequirePermission("control_plane.manage")]
[Route("api/v{version:apiVersion}/control-plane/demo")]
public sealed class DemoController(ISender sender, ICurrentActorProvider actor) : BaseController
{
    [HttpPost("reset")]
    public async Task<IActionResult> Reset(CancellationToken ct)
        => HandleResponse(await sender.Send(new ResetDemoCommand(actor.ActorId),ct).ConfigureAwait(false));

    [HttpPost("enter")]
    public async Task<IActionResult> Enter(CancellationToken cancellationToken, [FromQuery] bool readOnly = false)
        => HandleResponse(await sender.Send(new EnterDemoCommand(actor.ActorId, readOnly), cancellationToken).ConfigureAwait(false));
}
