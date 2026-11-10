using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.Api.Common.Authorization;
using ST.Api.Common.Controllers;
using ST.Application.Features.ControlPlane.Support;
using ST.SharedKernel.Features.ControlPlane.Support;
namespace ST.Api.Features.ControlPlane.Support.Controllers;
[ApiController,ApiVersion("1.0"),Authorize,RequirePermission("control_plane.manage")]
[Route("api/v{version:apiVersion}/control-plane/support")]
public sealed class PlatformSupportController(ISender sender) : BaseController
{
 [HttpGet("{id:guid}/audit")]
 public async Task<IActionResult> Audit(Guid id,CancellationToken ct)=>HandleResponse(await sender.Send(new GetSupportAuditQuery(id,true),ct).ConfigureAwait(false));
 [HttpGet] public async Task<IActionResult> List(CancellationToken ct)=>HandleResponse(await sender.Send(new GetSupportGrantsQuery(true),ct).ConfigureAwait(false));
 [HttpPost] public async Task<IActionResult> CreateRequest(SupportRequest request,CancellationToken ct)=>HandleResponse(await sender.Send(new SupportCommand("request",null,request),ct).ConfigureAwait(false));
 [HttpPost("{id:guid}/enter")] public async Task<IActionResult> Enter(Guid id,CancellationToken ct)=>HandleResponse(await sender.Send(new SupportCommand("enter",id,null),ct).ConfigureAwait(false));
 [HttpPost("{id:guid}/revoke")] public async Task<IActionResult> Revoke(Guid id,CancellationToken ct)=>HandleResponse(await sender.Send(new SupportCommand("revoke",id,null),ct).ConfigureAwait(false));
}
