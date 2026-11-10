using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.Api.Common.Authorization;
using ST.Api.Common.Controllers;
using ST.Application.Features.ControlPlane.Modules;
using ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
using ST.Domain.Shared.Contracts.Common;
namespace ST.Api.Features.ControlPlane.Modules.Controllers;

[ApiController, ApiVersion("1.0"), Authorize, RequirePermission("control_plane.manage")]
[Route("api/v{version:apiVersion}/control-plane/modules")]
public sealed class ModulesController(ISender sender, ICurrentActorProvider actor) : BaseController
{
    [HttpPost("{id:guid}/lifecycle")]
    public async Task<IActionResult> ChangeLifecycle(Guid id, ModuleLifecycleRequest request, CancellationToken cancellationToken) => HandleResponse(await sender.Send(new ChangeModuleLifecycleCommand(id, request, actor.ActorId), cancellationToken).ConfigureAwait(false));
    [HttpGet("{moduleKey}/catalogue")]
    public async Task<IActionResult> Catalogue(string moduleKey, CancellationToken cancellationToken) => HandleResponse(await sender.Send(new GetModuleCatalogueQuery(moduleKey), cancellationToken).ConfigureAwait(false));
    [HttpPost("{moduleKey}/catalogue")]
    public async Task<IActionResult> CreateEntry(string moduleKey, SaveCatalogueEntryRequest request, CancellationToken cancellationToken) => HandleResponse(await sender.Send(new SaveCatalogueEntryCommand(moduleKey, null, request, actor.ActorId), cancellationToken).ConfigureAwait(false));
    [HttpPut("{moduleKey}/catalogue/{entryId:guid}")]
    public async Task<IActionResult> UpdateEntry(string moduleKey, Guid entryId, SaveCatalogueEntryRequest request, CancellationToken cancellationToken) => HandleResponse(await sender.Send(new SaveCatalogueEntryCommand(moduleKey, entryId, request, actor.ActorId), cancellationToken).ConfigureAwait(false));
    [HttpGet("{moduleKey}/flags")]
    public async Task<IActionResult> Flags(string moduleKey, CancellationToken cancellationToken) => HandleResponse(await sender.Send(new GetModuleFlagsQuery(moduleKey), cancellationToken).ConfigureAwait(false));
    [HttpPut("{moduleKey}/flags")]
    public async Task<IActionResult> SaveFlag(string moduleKey, SaveModuleFlagRequest request, CancellationToken cancellationToken) => HandleResponse(await sender.Send(new SaveModuleFlagCommand(moduleKey, request, actor.ActorId), cancellationToken).ConfigureAwait(false));
    [HttpGet] public async Task<IActionResult> Get(CancellationToken cancellationToken) => HandleResponse(await sender.Send(new GetModulesQuery(), cancellationToken).ConfigureAwait(false));
    [HttpPost] public async Task<IActionResult> Create(SaveModuleRequest request, CancellationToken cancellationToken) => HandleResponse(await sender.Send(new SaveModuleCommand(null, request, actor.ActorId), cancellationToken).ConfigureAwait(false));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, SaveModuleRequest request, CancellationToken cancellationToken) => HandleResponse(await sender.Send(new SaveModuleCommand(id, request, actor.ActorId), cancellationToken).ConfigureAwait(false));
}
