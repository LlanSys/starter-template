using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.Api.Common.Authorization;
using ST.Api.Common.Controllers;
using ST.Application.Features.ControlPlane.Support;
using ST.SharedKernel.Features.ControlPlane.Support;
namespace ST.Api.Features.IAM.Support.Controllers;
[ApiController,ApiVersion("1.0"),Authorize,RequireModule("iam")]
[Route("api/v{version:apiVersion}/iam/support")]
public sealed class TenantSupportController(ISender sender) : BaseController
{
 [HttpGet("{id:guid}/audit"),RequirePermission("support.approve")]
 public async Task<IActionResult> Audit(Guid id,CancellationToken ct)=>HandleResponse(await sender.Send(new GetSupportAuditQuery(id),ct).ConfigureAwait(false));
 [HttpGet,RequirePermission("support.approve")] public async Task<IActionResult> List(CancellationToken ct)=>HandleResponse(await sender.Send(new GetSupportGrantsQuery(),ct).ConfigureAwait(false));
 [HttpPost("{id:guid}/approve"),RequirePermission("support.approve")] public async Task<IActionResult> Approve(Guid id,SupportRequest request,CancellationToken ct)=>HandleResponse(await sender.Send(new SupportCommand("approve",id,request),ct).ConfigureAwait(false));
 [HttpPost("{id:guid}/revoke"),RequirePermission("support.approve")] public async Task<IActionResult> Revoke(Guid id,CancellationToken ct)=>HandleResponse(await sender.Send(new SupportCommand("revoke",id,null),ct).ConfigureAwait(false));
 [HttpPost("{id:guid}/exit")] public async Task<IActionResult> Exit(Guid id,CancellationToken ct)=>HandleResponse(await sender.Send(new SupportCommand("exit",id,null),ct).ConfigureAwait(false));
}
