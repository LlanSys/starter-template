using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.HR.Departments.Dtos;
using ST.UI.Blazor.Configuration;
using ST.UI.Blazor.Features.Shared.BackendApi;
using ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;
using ST.UI.Rcl.Features.HR.Departments.Contracts.Interfaces;
using Microsoft.Extensions.Options;

namespace ST.UI.Blazor.Features.HR.Departments.Contracts.Implementations;

internal sealed class DepartmentService(IBackendApiClient apiClient, IOptions<BackendApiSettings> apiSettings) : IDepartmentService
{
    private readonly BackendApiSettings _apiSettings = apiSettings.Value;

    public Task<AppResponse<IReadOnlyList<DepartmentResponse>>> GetAsync()
        => apiClient.SendAsync<IReadOnlyList<DepartmentResponse>>(
            HttpMethod.Get,
            Format(_apiSettings.Endpoints.Hr.DepartmentsActive),
            unavailableMessage: "The department service is unavailable. Please try again.",
            timeoutMessage: "The department service timed out. Please try again.");

    public Task<AppResponse<PagedResponse<DepartmentResponse, Guid>>> SearchAsync(DepartmentSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return apiClient.SendAsync<PagedResponse<DepartmentResponse, Guid>>(
            HttpMethod.Get,
            Format(_apiSettings.Endpoints.Hr.Departments, queryString: request.BuildQueryString()),
            unavailableMessage: "The department service is unavailable. Please try again.",
            timeoutMessage: "The department service timed out. Please try again.");
    }

    public Task<AppResponse<DepartmentResponse>> GetByIdAsync(Guid id)
        => apiClient.SendAsync<DepartmentResponse>(
            HttpMethod.Get,
            Format(_apiSettings.Endpoints.Hr.DepartmentDetail, ("id", id.ToString())),
            unavailableMessage: "The department service is unavailable. Please try again.",
            timeoutMessage: "The department service timed out. Please try again.");

    public Task<AppResponse<DepartmentResponse>> CreateAsync(CreateDepartmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return apiClient.SendAsync<DepartmentResponse>(
            HttpMethod.Post,
            Format(_apiSettings.Endpoints.Hr.DepartmentCreate),
            request,
            unavailableMessage: "The department service is unavailable. Please try again.",
            timeoutMessage: "The department service timed out. Please try again.");
    }

    public Task<AppResponse<DepartmentResponse>> UpdateAsync(Guid id, UpdateDepartmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return apiClient.SendAsync<DepartmentResponse>(
            HttpMethod.Put,
            Format(_apiSettings.Endpoints.Hr.DepartmentUpdate, ("id", id.ToString())),
            request,
            unavailableMessage: "The department service is unavailable. Please try again.",
            timeoutMessage: "The department service timed out. Please try again.");
    }

    public Task<AppResponse<bool>> DeleteAsync(Guid id)
        => apiClient.SendAsync<bool>(
            HttpMethod.Delete,
            Format(_apiSettings.Endpoints.Hr.DepartmentDelete, ("id", id.ToString())),
            unavailableMessage: "The department service is unavailable. Please try again.",
            timeoutMessage: "The department service timed out. Please try again.");

    private string Format(string endpoint, params (string Key, string Value)[] parameters)
        => EndpointFormatter.Format(endpoint, _apiSettings.Version, parameters.ToDictionary(static item => item.Key, static item => item.Value));

    private string Format(string endpoint, string? queryString)
        => EndpointFormatter.Format(endpoint, _apiSettings.Version, queryString: queryString);
}
