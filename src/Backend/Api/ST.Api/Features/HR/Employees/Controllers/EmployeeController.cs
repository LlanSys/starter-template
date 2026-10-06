using Asp.Versioning;
using ST.Api.Common.Authorization;
using ST.Application.Features.HR.Employees.CommandHandlers;
using ST.Application.Features.HR.Employees.QueryHandlers;
using ST.Api.Common.Controllers;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Employees.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ST.Api.Features.HR.Employees.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hr/employees")]
[ApiController]
public sealed class EmployeeController(ISender sender) : BaseController
{
    [HttpGet]
    [Authorize]
    [RequirePermission("employees.view")]
    public async Task<ActionResult<AppResponse<PagedResponse<EmployeeResponse, Guid>>>> Search([FromQuery] EmployeeSearchRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ArgumentNullException.ThrowIfNull(userId, nameof(userId));

        var response = await sender.Send(new SearchEmployeesQuery(request, userId)).ConfigureAwait(false);
        return !response.IsSuccess ? BadRequest(response) : Ok(response);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [RequirePermission("employees.view")]
    public async Task<ActionResult<AppResponse<EmployeeResponse>>> GetById(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ArgumentNullException.ThrowIfNull(userId, nameof(userId));

        var response = await sender.Send(new GetEmployeeByIdQuery(id, userId)).ConfigureAwait(false);
        return !response.IsSuccess ? NotFound(response) : Ok(response);
    }

    [HttpPost]
    [Authorize]
    [RequirePermission("employees.create")]
    public async Task<ActionResult<AppResponse<EmployeeResponse>>> Create(CreateEmployeeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ArgumentNullException.ThrowIfNull(userId, nameof(userId));

        var command = new CreateEmployeeCommand(request, userId);

        var response = await sender.Send(command).ConfigureAwait(false);
        return !response.IsSuccess ? BadRequest(response) : Ok(response);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    [RequirePermission("employees.edit")]
    public async Task<ActionResult<AppResponse<EmployeeResponse>>> Update(Guid id, UpdateEmployeeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ArgumentNullException.ThrowIfNull(userId, nameof(userId));

        var response = await sender.Send(new UpdateEmployeeCommand(id, request with { Id = id }, userId)).ConfigureAwait(false);
        return !response.IsSuccess ? BadRequest(response) : Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    [RequirePermission("employees.delete")]
    public async Task<ActionResult<AppResponse<bool>>> Delete(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ArgumentNullException.ThrowIfNull(userId, nameof(userId));

        var response = await sender.Send(new DeleteEmployeeCommand(id, userId)).ConfigureAwait(false);
        return !response.IsSuccess ? BadRequest(response) : Ok(response);
    }
}
