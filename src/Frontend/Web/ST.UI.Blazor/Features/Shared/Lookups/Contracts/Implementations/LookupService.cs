using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Lookups.Dtos;
using ST.UI.Blazor.Configuration;
using ST.UI.Blazor.Features.Shared.BackendApi;
using ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;
using ST.UI.Blazor.Features.Shared.Lookups.Contracts.Interfaces;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace ST.UI.Blazor.Features.Shared.Lookups.Contracts.Implementations;

internal sealed class LookupService(IBackendApiClient apiClient, IOptions<BackendApiSettings> apiSettings) : ILookupService
{
    private readonly BackendApiSettings _apiSettings = apiSettings.Value;

    public Task<AppResponse<IReadOnlyList<LookupCatalogTypeResponse>>> GetCatalogTypesAsync()
        => apiClient.SendAsync<IReadOnlyList<LookupCatalogTypeResponse>>(
            HttpMethod.Get,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Shared.LookupCatalogTypes,
                _apiSettings.Version),
            unavailableMessage: "The lookup catalog service is unavailable. Please try again.",
            timeoutMessage: "The lookup catalog service timed out. Please try again.");

    public Task<AppResponse<IReadOnlyList<LookupResponse>>> GetAsync(string lookupType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lookupType);

        return apiClient.SendAsync<IReadOnlyList<LookupResponse>>(
            HttpMethod.Get,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Shared.LookupByType,
                _apiSettings.Version,
                new Dictionary<string, string> { ["lookupType"] = lookupType }),
            unavailableMessage: "The lookup service is unavailable. Please try again.",
            timeoutMessage: "The lookup service timed out. Please try again.");
    }

    public Task<AppResponse<LookupResponse>> CreateAsync(string lookupType, CreateLookupRequest request)
        => apiClient.SendAsync<LookupResponse>(
            HttpMethod.Post,
            Format(_apiSettings.Endpoints.Shared.LookupCreate, lookupType),
            request,
            unavailableMessage: "The lookup service is unavailable. Please try again.",
            timeoutMessage: "The lookup service timed out. Please try again.");

    public Task<AppResponse<LookupResponse>> UpdateAsync(string lookupType, int id, UpdateLookupRequest request)
        => apiClient.SendAsync<LookupResponse>(
            HttpMethod.Put,
            Format(_apiSettings.Endpoints.Shared.LookupUpdate, lookupType, id),
            request,
            unavailableMessage: "The lookup service is unavailable. Please try again.",
            timeoutMessage: "The lookup service timed out. Please try again.");

    public Task<AppResponse<bool>> DeleteAsync(string lookupType, int id)
        => apiClient.SendAsync<bool>(
            HttpMethod.Delete,
            Format(_apiSettings.Endpoints.Shared.LookupDelete, lookupType, id),
            unavailableMessage: "The lookup service is unavailable. Please try again.",
            timeoutMessage: "The lookup service timed out. Please try again.");

    private string Format(string endpoint, string lookupType, int? id = null)
    {
        var parameters = new Dictionary<string, string>
        {
            ["lookupType"] = lookupType
        };

        if (id.HasValue)
        {
            parameters["id"] = id.Value.ToString(CultureInfo.InvariantCulture);
        }

        return EndpointFormatter.Format(endpoint, _apiSettings.Version, parameters);
    }
}
