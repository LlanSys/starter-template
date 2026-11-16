using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.Api.Common.Authorization;
using ST.Api.Common.Controllers;
using ST.Application.Features.ControlPlane.Staff;
using ST.Application.Features.ControlPlane.Staff.Commands;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.ControlPlane.Staff;

namespace ST.Api.Features.ControlPlane.Staff.Controllers;

[ApiController, ApiVersion("1.0"), Authorize]
[Route("api/v{version:apiVersion}/control-plane/staff")]
public sealed class PlatformStaffController(IPlatformStaffService service, ISender sender) : BaseController
{
    [HttpGet, RequirePermission(PlatformPermissions.ViewPlatformStaff)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        CancellationToken ct = default
    )
    {
        return HandleResponse(await service.ListAsync(search, page, ct).ConfigureAwait(false));
    }

    [HttpGet("profiles"), RequirePermission(PlatformPermissions.ViewPlatformStaff)]
    public IActionResult Profiles()
    {
        return HandleResponse(AppResponses.Success(PlatformStaffProfiles.All));
    }

    [HttpGet("{userId}/history"), RequirePermission(PlatformPermissions.ViewPlatformStaff)]
    public async Task<IActionResult> History(string userId, CancellationToken ct)
    {
        return HandleResponse(await service.HistoryAsync(userId, ct).ConfigureAwait(false));
    }

    [HttpPost, RequirePermission(PlatformPermissions.ManagePlatformStaff)]
    public async Task<IActionResult> Add(CreatePlatformStaffRequest request, CancellationToken ct)
    {
        return HandleResponse(await sender.Send(new CreatePlatformStaffCommand(request), ct).ConfigureAwait(false));
    }

    [HttpPut("{userId}"), RequirePermission(PlatformPermissions.ManagePlatformStaff)]
    public async Task<IActionResult> Save(string userId, UpdatePlatformStaffRequest request, CancellationToken ct)
    {
        return HandleResponse(await sender.Send(new UpdatePlatformStaffCommand(userId, request), ct).ConfigureAwait(false));
    }

    [HttpPost("{userId}/invitation"), RequirePermission(PlatformPermissions.ManagePlatformStaff)]
    public async Task<IActionResult> Invite(string userId, PlatformStaffInvitationRequest request, CancellationToken ct)
    {
        return HandleResponse(await sender.Send(new InvitePlatformStaffCommand(userId, request), ct).ConfigureAwait(false));
    }
}
