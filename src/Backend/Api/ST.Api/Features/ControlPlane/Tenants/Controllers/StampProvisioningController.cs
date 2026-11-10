using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.Api.Common.Authorization;
using ST.Api.Common.Controllers;
using ST.Api.Features.ControlPlane.Tenants.Dtos;
using ST.Application.Features.ControlPlane.Tenants.Commands;

namespace ST.Api.Features.ControlPlane.Tenants.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/control-plane/provisioning/{tenantId:guid}/attempts/{operationId:guid}")]
[Authorize(Policy = StampProvisioningAuthentication.Policy)]
public sealed class StampProvisioningController(ISender sender) : BaseController
{
    [HttpPost]
    public async Task<IActionResult> Complete(
        Guid tenantId,
        Guid operationId,
        CompleteTenantProvisioningRequest request,
        CancellationToken cancellationToken)
    {
        if (request.OperationId.HasValue && request.OperationId.Value != operationId)
        {
            return BadRequest("The provisioning attempt does not match this request.");
        }

        return HandleResponse(await sender.Send(new CompleteTenantProvisioningCommand(
            tenantId, request.DatabaseConnectionString, request.ApplicationInsightsKey,
            operationId, request.Succeeded, request.DatabaseProvider, request.ClientOrigin), cancellationToken).ConfigureAwait(false));
    }
}
