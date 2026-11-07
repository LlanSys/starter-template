using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ST.Api.Common.Controllers;
using ST.Application.Features.ControlPlane.Tenants.Queries;
namespace ST.Api.Features.ControlPlane.Tenants.Controllers;
[ApiController, ApiVersion("1.0"), AllowAnonymous]
[Route("api/v{version:apiVersion}/workspaces")]
public sealed class PublicWorkspaceController(ISender sender) : BaseController
{
    // Public routing metadata only. Credentials and every protected operation remain tenant scoped.
    [HttpGet("resolve"), EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> Resolve([FromQuery] string? identifier, [FromQuery] Guid? tenantId, CancellationToken cancellationToken) => HandleResponse(await sender.Send(new ResolveWorkspaceQuery(identifier, tenantId), cancellationToken).ConfigureAwait(false));
}
