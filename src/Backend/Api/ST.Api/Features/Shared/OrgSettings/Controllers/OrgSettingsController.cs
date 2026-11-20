using Asp.Versioning;
using ST.Api.Common.Authorization;
using ST.Api.Common.Controllers;
using ST.Application.Features.Shared.OrgSettings.CommandHandlers;
using ST.Application.Features.Shared.OrgSettings.QueryHandlers;
using ST.Application.Features.Shared.OrgSettings.SaveCatalogSetting;
using ST.SharedKernel.Features.Shared.OrgSettings.Dtos;
using MediatR;
using ST.SharedKernel.Features.Shared.OrgSettings;
using ST.SharedKernel.Dtos.Common;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ST.Api.Features.Shared.OrgSettings.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/shared/tenant-settings")]
[ApiController]
[Authorize]
public sealed class OrgSettingsController(ISender sender) : BaseController
{
    [HttpGet("catalogue")]
    [RequirePermission("tenant-settings.view")]
    public async Task<IActionResult> GetCatalogue(CancellationToken ct)
    {
        return HandleResponse(await sender.Send(new GetCatalogOrgSettingsQuery(), ct).ConfigureAwait(false));
    }

    [HttpGet("presentation")]
    public async Task<IActionResult> GetPresentation(CancellationToken ct)
    {
        var response = await sender.Send(new GetCatalogOrgSettingsQuery(), ct).ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            return HandleResponse(response);
        }
        var values = response.Data.ToDictionary(item => item.Definition.Key, item => item.Value);
        return HandleResponse(AppResponses.Success(new PresentationSettingsResponse(
            values[OrganizationSettingCatalog.TimeZone], values[OrganizationSettingCatalog.RegionalFormat],
            values[OrganizationSettingCatalog.DateFormat], values[OrganizationSettingCatalog.TimeFormat],
            int.Parse(values[OrganizationSettingCatalog.PageSize], CultureInfo.InvariantCulture))));
    }

    [HttpGet("history")]
    [RequirePermission("tenant-settings.view")]
    public async Task<IActionResult> GetHistory(CancellationToken ct)
    {
        return HandleResponse(await sender.Send(new GetOrgSettingChangesQuery(), ct).ConfigureAwait(false));
    }

    [HttpPut("catalogue/{key}")]
    [RequirePermission("tenant-settings.update")]
    public async Task<IActionResult> SaveCatalogue(string key, SaveCatalogOrgSettingRequest request, CancellationToken ct)
    {
        return HandleResponse(await sender.Send(new SaveCatalogOrgSettingCommand(key, request.Value, request.Revision, false, GetUserId()), ct).ConfigureAwait(false));
    }

    [HttpPost("catalogue/{key}/reset")]
    [RequirePermission("tenant-settings.delete")]
    public async Task<IActionResult> ResetCatalogue(string key, SaveCatalogOrgSettingRequest request, CancellationToken ct)
    {
        return HandleResponse(await sender.Send(new SaveCatalogOrgSettingCommand(key, null, request.Revision, true, GetUserId()), ct).ConfigureAwait(false));
    }

    [HttpGet]
    [RequirePermission("tenant-settings.view")]
    public async Task<IActionResult> GetOrgSettings(CancellationToken ct)
    {
        var response = await sender.Send(new GetOrgSettingsQuery(), ct).ConfigureAwait(false);
        return HandleResponse(response);
    }

    [HttpGet("{key}")]
    [RequirePermission("tenant-settings.view")]
    public async Task<IActionResult> GetOrgSettingByKey(string key, CancellationToken ct)
    {
        var response = await sender.Send(new GetOrgSettingByKeyQuery(key), ct).ConfigureAwait(false);
        return HandleResponse(response);
    }

    [HttpPost]
    [RequirePermission("tenant-settings.create")]
    public async Task<IActionResult> CreateOrgSetting([FromBody] CreateOrgSettingRequest request, CancellationToken ct)
    {
        var response = await sender.Send(new CreateOrgSettingCommand(request, GetUserId()), ct).ConfigureAwait(false);
        return HandleResponse(response);
    }

    [HttpPut]
    [RequirePermission("tenant-settings.update")]
    public async Task<IActionResult> UpdateOrgSetting([FromBody] UpdateOrgSettingRequest request, CancellationToken ct)
    {
        var response = await sender.Send(new UpdateOrgSettingCommand(request, GetUserId()), ct).ConfigureAwait(false);
        return HandleResponse(response);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("tenant-settings.delete")]
    public async Task<IActionResult> DeleteOrgSetting(Guid id, CancellationToken ct)
    {
        var response = await sender.Send(new DeleteOrgSettingCommand(id, GetUserId()), ct).ConfigureAwait(false);
        return HandleResponse(response);
    }

    private string GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("User context is not available.");
        }
        return userId;
    }
}
