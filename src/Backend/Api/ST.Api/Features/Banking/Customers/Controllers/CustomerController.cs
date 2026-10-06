using Asp.Versioning;
using ST.Api.Common.Authorization;
using ST.Application.Features.Banking.Customers.CommandHandlers;
using ST.Application.Features.Banking.Customers.QueryHandlers;
using ST.Api.Common.Controllers;
using ST.SharedKernel.Features.Banking.Customers.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ST.Api.Features.Banking.Customers.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/banking/customers")]
[ApiController]
[Authorize]
public sealed class CustomerController(ISender sender) : BaseController
{
    [HttpGet]
    [RequirePermission("customers.view")]
    public async Task<IActionResult> List([FromQuery] CustomerSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ArgumentNullException.ThrowIfNull(userId, nameof(userId));

        var response = await sender.Send(new SearchCustomerListQuery(request, userId)).ConfigureAwait(false);
        return HandleResponse(response);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("customers.view")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var response = await sender.Send(new GetCustomerByIdQuery(id)).ConfigureAwait(false);
        return HandleResponse(response);
    }

    [HttpPost]
    [RequirePermission("customers.create")]
    public async Task<IActionResult> Create(CreateCustomerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ArgumentNullException.ThrowIfNull(userId, nameof(userId));

        var command = new CreateCustomerCommand(request, userId);

        var response = await sender.Send(command).ConfigureAwait(false);
        return HandleResponse(response);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("customers.edit")]
    public async Task<IActionResult> Update(Guid id, UpdateCustomerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ArgumentNullException.ThrowIfNull(userId, nameof(userId));

        var command = new UpdateCustomerCommand(id, request with { Id = id }, userId);
        var response = await sender.Send(command).ConfigureAwait(false);
        return HandleResponse(response);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("customers.delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ArgumentNullException.ThrowIfNull(userId, nameof(userId));

        var response = await sender.Send(new DeleteCustomerCommand(id, userId)).ConfigureAwait(false);
        return HandleResponse(response);
    }
}
