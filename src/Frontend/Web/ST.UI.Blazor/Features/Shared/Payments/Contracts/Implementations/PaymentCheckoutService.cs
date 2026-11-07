using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Payments.Dtos;
using ST.UI.Blazor.Configuration;
using ST.UI.Blazor.Features.Shared.BackendApi;
using ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;
using ST.UI.Blazor.Features.Shared.Payments.Contracts.Interfaces;
using Microsoft.Extensions.Options;

namespace ST.UI.Blazor.Features.Shared.Payments.Contracts.Implementations;

internal sealed class PaymentCheckoutService(
    IBackendApiClient apiClient,
    IOptions<BackendApiSettings> apiSettings) : IPaymentCheckoutService
{
    private readonly BackendApiSettings _apiSettings = apiSettings.Value;

    public Task<AppResponse<PaymentInitiationResponse>> CreateCheckoutAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return apiClient.SendAsync<PaymentInitiationResponse>(
            HttpMethod.Post,
            EndpointFormatter.Format(_apiSettings.Endpoints.Shared.Payments.Checkout, _apiSettings.Version),
            request,
            unavailableMessage: "The payment service is unavailable. Please try again.",
            timeoutMessage: "The payment service timed out. Please try again.",
            cancellationToken: cancellationToken);
    }

    public Task<AppResponse<PaymentStatusResponse>> GetStatusAsync(
        string provider,
        string paymentReference,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentReference);

        return apiClient.SendAsync<PaymentStatusResponse>(
            HttpMethod.Get,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Shared.Payments.Status,
                _apiSettings.Version,
                new Dictionary<string, string>
                {
                    ["provider"] = provider,
                    ["paymentReference"] = paymentReference
                }),
            unavailableMessage: "The payment service is unavailable. Please try again.",
            timeoutMessage: "The payment service timed out. Please try again.",
            cancellationToken: cancellationToken);
    }
    public Task<AppResponse<IReadOnlyCollection<PaymentProviderCapabilityResponse>>> GetCapabilitiesAsync(
        CancellationToken cancellationToken = default)
    {
        return apiClient.SendAsync<IReadOnlyCollection<PaymentProviderCapabilityResponse>>(
            HttpMethod.Get,
            EndpointFormatter.Format(_apiSettings.Endpoints.Shared.Payments.Capabilities, _apiSettings.Version),
            unavailableMessage: "The payment service is unavailable. Please try again.",
            timeoutMessage: "The payment service timed out. Please try again.",
            cancellationToken: cancellationToken);
    }

    public Task<AppResponse<PaymentHistoryResponse>> GetHistoryAsync(
        PaymentHistoryFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string>();

        if (request.Cursor.HasValue) queryParams["cursor"] = request.Cursor.Value.ToString();
        queryParams["pageSize"] = request.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(request.SearchTerm)) queryParams["searchTerm"] = request.SearchTerm;
        if (!string.IsNullOrWhiteSpace(request.Provider)) queryParams["provider"] = request.Provider;
        if (request.ExactAmount.HasValue) queryParams["exactAmount"] = request.ExactAmount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (request.MinAmount.HasValue) queryParams["minAmount"] = request.MinAmount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (request.MaxAmount.HasValue) queryParams["maxAmount"] = request.MaxAmount.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (request.StartDate.HasValue) queryParams["startDate"] = request.StartDate.Value.ToString("O");
        if (request.EndDate.HasValue) queryParams["endDate"] = request.EndDate.Value.ToString("O");
        if (!string.IsNullOrWhiteSpace(request.Status)) queryParams["status"] = request.Status;

        return apiClient.SendAsync<PaymentHistoryResponse>(
            HttpMethod.Get,
            EndpointFormatter.Format(
                _apiSettings.Endpoints.Shared.Payments.History,
                _apiSettings.Version,
                queryParams),
            unavailableMessage: "The payment service is unavailable. Please try again.",
            timeoutMessage: "The payment service timed out. Please try again.",
            cancellationToken: cancellationToken);
    }

    public Task<AppResponse<string>> RegisterMpesaC2BUrlsAsync(
        CancellationToken cancellationToken = default)
    {
        return apiClient.SendAsync<string>(
            HttpMethod.Post,
            EndpointFormatter.Format(_apiSettings.Endpoints.Shared.Payments.RegisterMpesaC2BUrls, _apiSettings.Version),
            unavailableMessage: "The payment service is unavailable. Please try again.",
            timeoutMessage: "The payment service timed out. Please try again.",
            cancellationToken: cancellationToken);
    }

    public Task<AppResponse<string>> SimulateMpesaC2BAsync(
        SimulateMpesaC2BRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return apiClient.SendAsync<string>(
            HttpMethod.Post,
            EndpointFormatter.Format(_apiSettings.Endpoints.Shared.Payments.SimulateMpesaC2B, _apiSettings.Version),
            request,
            unavailableMessage: "The payment service is unavailable. Please try again.",
            timeoutMessage: "The payment service timed out. Please try again.",
            cancellationToken: cancellationToken);
    }
}
