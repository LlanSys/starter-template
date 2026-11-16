using ST.Application.Features.ControlPlane.Tenants.Contracts;
using MediatR;
using ST.Application.Features.ControlPlane.Kyc.Commands;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ST.Api.Common.Controllers;
using ST.Api.Features.ControlPlane.Kyc.Dtos;
using ST.Application.Features.ControlPlane.Kyc.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

namespace ST.Api.Features.ControlPlane.Kyc.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/onboarding/kyc/{tenantId:guid}")]
[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class KycSubmissionController(IKycEvidenceService evidence, ISender sender, IFrontendTenantBinding binding) : BaseController
{
    [HttpPost("inspect")]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> Inspect(Guid tenantId, InspectKycSubmissionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (binding.TenantId is Guid bound && bound != tenantId)
        {
            return NotFound();
        }
        return HandleResponse(await evidence.InspectLinkAsync(tenantId, request.Token, cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("documents")]
    [EnableRateLimiting("FileUploadPolicy")]
    [RequestSizeLimit(11_534_336)]
    [RequestFormLimits(MultipartBodyLengthLimit = 11_534_336)]
    public async Task<IActionResult> Upload(Guid tenantId, [FromForm] KycUploadForm form, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);

        if (binding.TenantId is Guid bound && bound != tenantId)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(form.Token))
        {
            return HandleResponse(AppResponses.Failure<Guid>("This submission link is invalid, expired, or closed."));
        }

        using var content = form.File.OpenReadStream();
        return HandleResponse(await sender.Send(new UploadKycDocumentCommand(tenantId, form.Token, form.DocumentType,
            form.File.FileName, form.File.Length, content, form.OtherDocumentName), cancellationToken).ConfigureAwait(false));
    }
}
