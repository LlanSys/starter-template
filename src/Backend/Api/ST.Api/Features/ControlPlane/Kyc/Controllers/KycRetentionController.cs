using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.Api.Common.Authorization;
using ST.Api.Common.Controllers;
using ST.Application.Features.ControlPlane.Kyc.Commands;
using ST.Application.Features.ControlPlane.Kyc.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

namespace ST.Api.Features.ControlPlane.Kyc.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/control-plane/kyc/retention")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class KycRetentionController(IKycRetentionService retention, ISender sender) : BaseController
{
    [HttpGet("policies")]
    [RequirePermission(PlatformPermissions.ViewKyc)]
    public async Task<IActionResult> Policies(CancellationToken cancellationToken)
    {
        return HandleResponse(await retention.PoliciesAsync(cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("policies")]
    [RequirePermission(PlatformPermissions.ManageKycRetention)]
    public async Task<IActionResult> Publish(PublishKycRetentionPolicyRequest request, CancellationToken cancellationToken)
    {
        return HandleResponse(await sender.Send(new PublishKycRetentionPolicyCommand(request), cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("cases/{tenantId:guid}")]
    [RequirePermission(PlatformPermissions.ViewKyc)]
    public async Task<IActionResult> Status(Guid tenantId, CancellationToken cancellationToken)
    {
        return HandleResponse(await retention.StatusAsync(tenantId, cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("cases/{tenantId:guid}")]
    [RequirePermission(PlatformPermissions.ManageKycRetention)]
    public async Task<IActionResult> Change(Guid tenantId, KycRetentionActionRequest request, CancellationToken cancellationToken)
    {
        if (request.Action is "hold" or "release") return HandleResponse(AppResponses.Failure<bool>("Use the legal-hold operation."));
        return HandleResponse(await sender.Send(new ActOnKycRetentionCommand(tenantId, request), cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("cases/{tenantId:guid}/hold")]
    [RequirePermission(PlatformPermissions.ManageKycHolds)]
    public async Task<IActionResult> Hold(Guid tenantId, KycRetentionActionRequest request, CancellationToken cancellationToken)
    {
        if (request.Action is not ("hold" or "release")) return HandleResponse(AppResponses.Failure<bool>("Choose a legal-hold operation."));
        return HandleResponse(await sender.Send(new ActOnKycRetentionCommand(tenantId, request), cancellationToken).ConfigureAwait(false));
    }
}
