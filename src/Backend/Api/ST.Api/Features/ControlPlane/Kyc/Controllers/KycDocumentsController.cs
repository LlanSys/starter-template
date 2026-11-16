using MediatR;
using ST.Application.Features.ControlPlane.Kyc.Commands;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ST.Api.Common.Authorization;
using ST.Api.Common.Controllers;
using ST.Api.Features.ControlPlane.Kyc.Dtos;
using ST.Application.Features.ControlPlane.Kyc.Contracts;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

namespace ST.Api.Features.ControlPlane.Kyc.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/control-plane/tenants/{tenantId:guid}/kyc")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class KycDocumentsController(IKycEvidenceService evidence, ISender sender) : BaseController
{
    [HttpGet("case")]
    [RequirePermission(PlatformPermissions.ViewKyc)]
    public async Task<IActionResult> Case(Guid tenantId, CancellationToken cancellationToken)
    {
        return HandleResponse(await evidence.CaseAsync(tenantId, cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("documents")]
    [RequirePermission(PlatformPermissions.ViewKyc)]
    public async Task<IActionResult> List(Guid tenantId, CancellationToken cancellationToken, [FromQuery] string? documentTypeKey = null)
    {
        return HandleResponse(await evidence.ListAsync(tenantId, cancellationToken, documentTypeKey).ConfigureAwait(false));
    }

    [HttpPost("submission-link")]
    [RequirePermission(PlatformPermissions.ManageKycSubmissions)]
    public async Task<IActionResult> IssueLink(Guid tenantId, CancellationToken cancellationToken)
    {
        return HandleResponse(await evidence.IssueLinkAsync(tenantId, cancellationToken).ConfigureAwait(false));
    }

    [HttpDelete("submission-link")]
    [RequirePermission(PlatformPermissions.ManageKycSubmissions)]
    public async Task<IActionResult> RevokeLink(Guid tenantId, CancellationToken cancellationToken)
    {
        return HandleResponse(await evidence.RevokeLinkAsync(tenantId, cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("documents")]
    [EnableRateLimiting("FileUploadPolicy")]
    [RequestSizeLimit(11_534_336)]
    [RequestFormLimits(MultipartBodyLengthLimit = 11_534_336)]
    [RequirePermission(PlatformPermissions.UploadKycDocuments)]
    public async Task<IActionResult> Upload(Guid tenantId, [FromForm] KycUploadForm form, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);
        using var content = form.File.OpenReadStream();
        return HandleResponse(await sender.Send(new UploadKycDocumentCommand(tenantId, null, form.DocumentType,
            form.File.FileName, form.File.Length, content, form.OtherDocumentName), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("documents/{documentId:guid}/reviews")]
    [RequirePermission(PlatformPermissions.ReviewKycDocuments)]
    public async Task<IActionResult> Review(Guid tenantId, Guid documentId, ReviewKycDocumentRequest request, CancellationToken cancellationToken)
    {
        return HandleResponse(await sender.Send(new ReviewKycDocumentCommand(tenantId, documentId, request), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("documents/{documentId:guid}/safety-retry")]
    [RequirePermission(PlatformPermissions.ReviewKycDocuments)]
    public async Task<IActionResult> RetrySafetyScan(Guid tenantId, Guid documentId, RetryKycSafetyScanRequest request, CancellationToken cancellationToken)
    {
        return HandleResponse(await sender.Send(new RetryKycSafetyScanCommand(tenantId, documentId, request), cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("documents/{documentId:guid}/content")]
    [RequirePermission(PlatformPermissions.DownloadKycDocuments)]
    public async Task<IActionResult> Download(Guid tenantId, Guid documentId, CancellationToken cancellationToken)
    {
        var result = await evidence.DownloadAsync(tenantId, documentId, cancellationToken).ConfigureAwait(false);
        return HandleResponse(result, onSuccess: response =>
        {
            Response.Headers.XContentTypeOptions = "nosniff";
            return File(response.Data!.Content, response.Data.ContentType, response.Data.FileName);
        });
    }
}
