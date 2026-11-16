using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.Api.Common.Authorization;
using ST.Api.Common.Controllers;
using ST.Application.Features.ControlPlane.Kyc.Contracts;
using ST.SharedKernel.Features.ControlPlane;

namespace ST.Api.Features.ControlPlane.Kyc.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/control-plane/kyc/cases")]
[Authorize]
[RequirePermission(PlatformPermissions.ViewKyc)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class KycCasesController(IKycEvidenceService evidence) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken, [FromQuery] string? search = null, [FromQuery] int page = 0)
    {
        return HandleResponse(await evidence.CasesAsync(search, page, cancellationToken).ConfigureAwait(false));
    }
}
