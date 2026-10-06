using Asp.Versioning;
using ST.Api.Common.Authorization;
using ST.Api.Common.Controllers;
using ST.Application.Features.ControlPlane.Stamps.Commands;
using ST.Application.Features.ControlPlane.Stamps.Queries;
using ST.SharedKernel.Features.ControlPlane.Stamps.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ST.Api.Features.ControlPlane.Stamps.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/control-plane/stamps")]
[ApiController]
[Authorize]
public sealed class DeploymentStampsController(ISender sender) : BaseController
{
    [HttpGet]
    [RequirePermission("control_plane.manage")]
    public async Task<IActionResult> GetAllDeploymentStamps()
        => HandleResponse(await sender.Send(new GetAllDeploymentStampsQuery()).ConfigureAwait(false));

    [HttpGet("{id:guid}")]
    [RequirePermission("control_plane.manage")]
    public async Task<IActionResult> GetDeploymentStampById(Guid id)
        => HandleResponse(await sender.Send(new GetDeploymentStampByIdQuery(id)).ConfigureAwait(false));

    [HttpPost]
    [RequirePermission("control_plane.manage")]
    public async Task<IActionResult> CreateDeploymentStamp(CreateDeploymentStampRequest request)
        => HandleResponse(await sender.Send(new CreateDeploymentStampCommand(request)).ConfigureAwait(false));

    [HttpPut("{id:guid}")]
    [RequirePermission("control_plane.manage")]
    public async Task<IActionResult> UpdateDeploymentStamp(Guid id, UpdateDeploymentStampRequest request)
        => HandleResponse(await sender.Send(new UpdateDeploymentStampCommand(id, request)).ConfigureAwait(false));
}
