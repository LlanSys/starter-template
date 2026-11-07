using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ST.Api.Common.Controllers;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;

namespace ST.Api.Features.ControlPlane.Tenants.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/workspaces/administrator-invitation")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdministratorInvitationController(IAdministratorInvitationService invitations) : BaseController
{
    [HttpPost("inspect")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> Inspect(InspectAdministratorInvitationRequest request, CancellationToken cancellationToken)
    {
        return HandleResponse(await invitations.InspectAsync(request, cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("accept")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> Accept(AcceptAdministratorInvitationRequest request, CancellationToken cancellationToken)
    {
        return HandleResponse(await invitations.AcceptPasswordAsync(request, cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("complete")]
    [Authorize]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> Complete(CancellationToken cancellationToken)
    {
        return HandleResponse(await invitations.CompleteAsync(cancellationToken).ConfigureAwait(false));
    }
}
