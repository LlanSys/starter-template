using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Banking.Customers.Dtos;
using ST.UI.Blazor.Configuration;
using ST.UI.Blazor.Features.Shared.BackendApi;
using ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;
using ST.UI.Rcl.Features.Banking.Customers.Contracts.Interfaces;
using Microsoft.Extensions.Options;

namespace ST.UI.Blazor.Features.Banking.Customers.Contracts.Implementations;

internal sealed class CustomerService(IBackendApiClient apiClient, IOptions<BackendApiSettings> apiSettings) : ICustomerService
{
    private readonly BackendApiSettings _apiSettings = apiSettings.Value;

    public Task<AppResponse<PagedResponse<CustomerResponse, Guid>>> SearchAsync(CustomerSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var endpoint = Format(_apiSettings.Endpoints.Banking.Customers.Search, queryString: request.BuildQueryString());
        return apiClient.SendAsync<PagedResponse<CustomerResponse, Guid>>(
            HttpMethod.Get,
            endpoint,
            unavailableMessage: "The customer service is unavailable. Please try again.",
            timeoutMessage: "The customer service timed out. Please try again.");
    }

    public Task<AppResponse<CustomerResponse>> GetByIdAsync(Guid id)
        => apiClient.SendAsync<CustomerResponse>(
            HttpMethod.Get,
            Format(_apiSettings.Endpoints.Banking.Customers.Detail, ("id", id.ToString())),
            unavailableMessage: "The customer service is unavailable. Please try again.",
            timeoutMessage: "The customer service timed out. Please try again.");

    public Task<AppResponse<CustomerResponse>> CreateAsync(CreateCustomerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return apiClient.SendAsync<CustomerResponse>(
            HttpMethod.Post,
            Format(_apiSettings.Endpoints.Banking.Customers.Create),
            request,
            unavailableMessage: "The customer service is unavailable. Please try again.",
            timeoutMessage: "The customer service timed out. Please try again.");
    }

    public Task<AppResponse<CustomerResponse>> UpdateAsync(Guid id, UpdateCustomerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return apiClient.SendAsync<CustomerResponse>(
            HttpMethod.Put,
            Format(_apiSettings.Endpoints.Banking.Customers.Update, ("id", id.ToString())),
            request,
            unavailableMessage: "The customer service is unavailable. Please try again.",
            timeoutMessage: "The customer service timed out. Please try again.");
    }

    public Task<AppResponse<bool>> DeleteAsync(Guid id)
        => apiClient.SendAsync<bool>(
            HttpMethod.Delete,
            Format(_apiSettings.Endpoints.Banking.Customers.Delete, ("id", id.ToString())),
            unavailableMessage: "The customer service is unavailable. Please try again.",
            timeoutMessage: "The customer service timed out. Please try again.");

    private string Format(string endpoint, params (string Key, string Value)[] parameters)
        => EndpointFormatter.Format(endpoint, _apiSettings.Version, parameters.ToDictionary(static item => item.Key, static item => item.Value));

    private string Format(string endpoint, string? queryString)
        => EndpointFormatter.Format(endpoint, _apiSettings.Version, queryString: queryString);
}
