using Asp.Versioning;
using ST.Api.Common.Authorization;
using ST.Api.Common.Controllers;
using ST.Application.Features.HR.Departments.CommandHandlers;
using ST.Application.Features.HR.Departments.QueryHandlers;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Departments.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ST.Api.Features.HR.Departments.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hr/departments")]
[ApiController]
public sealed class DepartmentController(ISender sender) : BaseController
{
    [HttpGet]
    [Authorize]
    [RequirePermission("departments.view")]
    public async Task<ActionResult<AppResponse<PagedResponse<DepartmentResponse, Guid>>>> Search([FromQuery] DepartmentSearchRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ArgumentNullException.ThrowIfNull(userId, nameof(userId));

        var response = await sender.Send(new SearchDepartmentsQuery(request, userId)).ConfigureAwait(false);
        return !response.IsSuccess ? BadRequest(response) : Ok(response);
    }

    [HttpGet("active")]
    [Authorize]
    [RequirePermission("departments.view")]
    public async Task<ActionResult<AppResponse<IReadOnlyList<DepartmentResponse>>>> GetActive()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ArgumentNullException.ThrowIfNull(userId, nameof(userId));

        var response = await sender.Send(new GetDepartmentsQuery(userId)).ConfigureAwait(false);
        return !response.IsSuccess ? BadRequest(response) : Ok(response);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [RequirePermission("departments.view")]
    public async Task<ActionResult<AppResponse<DepartmentResponse>>> GetById(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ArgumentNullException.ThrowIfNull(userId, nameof(userId));

        var response = await sender.Send(new GetDepartmentByIdQuery(id, userId)).ConfigureAwait(false);
        return !response.IsSuccess ? NotFound(response) : Ok(response);
    }

    [HttpPost]
    [Authorize]
    [RequirePermission("departments.create")]
    public async Task<ActionResult<AppResponse<DepartmentResponse>>> Create(CreateDepartmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ArgumentNullException.ThrowIfNull(userId, nameof(userId));

        var response = await sender.Send(new CreateDepartmentCommand(request, userId)).ConfigureAwait(false);
        return !response.IsSuccess ? BadRequest(response) : Ok(response);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    [RequirePermission("departments.edit")]
    public async Task<ActionResult<AppResponse<DepartmentResponse>>> Update(Guid id, UpdateDepartmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ArgumentNullException.ThrowIfNull(userId, nameof(userId));

        var response = await sender.Send(new UpdateDepartmentCommand(id, request with { Id = id }, userId)).ConfigureAwait(false);
        return !response.IsSuccess ? BadRequest(response) : Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    [RequirePermission("departments.delete")]
    public async Task<ActionResult<AppResponse<bool>>> Delete(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ArgumentNullException.ThrowIfNull(userId, nameof(userId));

        var response = await sender.Send(new DeleteDepartmentCommand(id, userId)).ConfigureAwait(false);
        return !response.IsSuccess ? BadRequest(response) : Ok(response);
    }
}
