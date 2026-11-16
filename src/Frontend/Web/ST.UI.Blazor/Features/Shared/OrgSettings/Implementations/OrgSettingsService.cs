using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.OrgSettings.Dtos;
using ST.UI.Blazor.Configuration;
using ST.UI.Blazor.Features.Shared.BackendApi;
using ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;
using ST.UI.Blazor.Features.Shared.OrgSettings.Contracts;
using Microsoft.Extensions.Options;

namespace ST.UI.Blazor.Features.Shared.OrgSettings.Implementations;

internal sealed class OrgSettingsService(IBackendApiClient apiClient, IOptions<BackendApiSettings> apiSettings) : IOrgSettingsService
{
    private readonly BackendApiSettings _apiSettings = apiSettings.Value;
    private string Root => EndpointFormatter.Format(_apiSettings.Endpoints.Shared.OrgSettings.Root, _apiSettings.Version);

    public Task<AppResponse<IReadOnlyList<CatalogOrgSettingResponse>>> GetCatalogueAsync(CancellationToken ct = default)
    {
        return apiClient.SendAsync<IReadOnlyList<CatalogOrgSettingResponse>>(HttpMethod.Get, Root + "/catalogue", cancellationToken: ct);
    }

    public Task<AppResponse<PresentationSettingsResponse>> GetPresentationAsync(CancellationToken ct = default)
    {
        return apiClient.SendAsync<PresentationSettingsResponse>(HttpMethod.Get, Root + "/presentation", cancellationToken: ct);
    }

    public Task<AppResponse<IReadOnlyList<OrgSettingChangeResponse>>> GetHistoryAsync(CancellationToken ct = default)
    {
        return apiClient.SendAsync<IReadOnlyList<OrgSettingChangeResponse>>(HttpMethod.Get, Root + "/history", cancellationToken: ct);
    }

    public Task<AppResponse<bool>> SaveAsync(string key, SaveCatalogOrgSettingRequest request, CancellationToken ct = default)
    {
        return apiClient.SendAsync<bool>(HttpMethod.Put, Root + "/catalogue/" + Uri.EscapeDataString(key), request, cancellationToken: ct);
    }

    public Task<AppResponse<bool>> ResetAsync(string key, SaveCatalogOrgSettingRequest request, CancellationToken ct = default)
    {
        return apiClient.SendAsync<bool>(HttpMethod.Post, Root + "/catalogue/" + Uri.EscapeDataString(key) + "/reset", request, cancellationToken: ct);
    }

    public Task<AppResponse<IEnumerable<OrgSettingResponse>>> GetOrgSettingsAsync(CancellationToken ct = default)
    {
        return apiClient.SendAsync<IEnumerable<OrgSettingResponse>>(
            HttpMethod.Get,
            EndpointFormatter.Format(_apiSettings.Endpoints.Shared.OrgSettings.Root, _apiSettings.Version),
            unavailableMessage: "The tenant settings service is unavailable. Please try again.",
            timeoutMessage: "The request timed out. Please try again.",
            cancellationToken: ct);
    }

    public Task<AppResponse<OrgSettingResponse>> GetOrgSettingByKeyAsync(string key, CancellationToken ct = default)
    {
        return apiClient.SendAsync<OrgSettingResponse>(
            HttpMethod.Get,
            EndpointFormatter.Format(_apiSettings.Endpoints.Shared.OrgSettings.Detail, _apiSettings.Version, new Dictionary<string, string> { { "key", key } }),
            unavailableMessage: "The tenant settings service is unavailable. Please try again.",
            timeoutMessage: "The request timed out. Please try again.",
            cancellationToken: ct);
    }

    public Task<AppResponse<OrgSettingResponse>> CreateOrgSettingAsync(CreateOrgSettingRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return apiClient.SendAsync<OrgSettingResponse>(
            HttpMethod.Post,
            EndpointFormatter.Format(_apiSettings.Endpoints.Shared.OrgSettings.Root, _apiSettings.Version),
            request,
            unavailableMessage: "The tenant settings service is unavailable. Please try again.",
            timeoutMessage: "The request timed out. Please try again.",
            cancellationToken: ct);
    }

    public Task<AppResponse<OrgSettingResponse>> UpdateOrgSettingAsync(UpdateOrgSettingRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return apiClient.SendAsync<OrgSettingResponse>(
            HttpMethod.Put,
            EndpointFormatter.Format(_apiSettings.Endpoints.Shared.OrgSettings.Root, _apiSettings.Version),
            request,
            unavailableMessage: "The tenant settings service is unavailable. Please try again.",
            timeoutMessage: "The request timed out. Please try again.",
            cancellationToken: ct);
    }

    public Task<AppResponse<bool>> DeleteOrgSettingAsync(Guid id, CancellationToken ct = default)
    {
        return apiClient.SendAsync<bool>(
            HttpMethod.Delete,
            EndpointFormatter.Format(_apiSettings.Endpoints.Shared.OrgSettings.Root, _apiSettings.Version) + $"/{id}",
            unavailableMessage: "The tenant settings service is unavailable. Please try again.",
            timeoutMessage: "The request timed out. Please try again.",
            cancellationToken: ct);
    }
}
