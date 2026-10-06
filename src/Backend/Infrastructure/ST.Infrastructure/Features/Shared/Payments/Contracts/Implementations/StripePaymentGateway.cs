using ST.Domain.Features.Shared.Payments.Entities;
using ST.Application.Features.Shared.Payments.Contracts.Interfaces;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Features.Shared.Payments.Contracts.Implementations.Stripe;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Payments.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ST.Infrastructure.Features.Shared.Payments.Contracts.Implementations;

internal sealed class StripePaymentGateway(
    IHttpClientFactory httpClientFactory,
    IOptions<PaymentSettings> options,
    ILogger<StripePaymentGateway> logger) : IPaymentGateway
{
    private readonly StripePaymentSettings _settings = options.Value.Stripe;

    public async Task<AppResponse<PaymentInitiationResponse>> InitiateAsync(
        PaymentRecord record,
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsConfigured())
        {
            return AppResponses.Failure<PaymentInitiationResponse>(
                AppError.DependencyUnavailable("Stripe payment provider is not configured for this environment."));
        }

        try
        {
            var providerRequest = StripeCheckoutSessionRequest.From(
                record.Amount.Amount,
                record.Amount.Currency,
                record.Description,
                record.CustomerReference,
                request.CallbackUrl,
                _settings.SuccessUrl,
                _settings.CancelUrl);

            using var httpClient = CreateClient();
            using var content = new FormUrlEncodedContent(providerRequest.ToFormFields());
            using var response = await httpClient
                .PostAsync(string.Empty, content, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                HttpClientLogDefinitions.LogExternalApiWarning(
                    logger,
                    "POST",
                    _settings.CheckoutSessionsEndpoint,
                    (int)response.StatusCode);

                return AppResponses.Failure<PaymentInitiationResponse>(
                    AppError.DependencyUnavailable("Stripe could not accept the payment initiation request."));
            }

            using var stream = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            var providerResponse = await JsonSerializer
                .DeserializeAsync<StripeCheckoutSessionResponse>(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (providerResponse is null || string.IsNullOrWhiteSpace(providerResponse.Id))
            {
                return AppResponses.Failure<PaymentInitiationResponse>(
                    AppError.DependencyUnavailable("Stripe returned an incomplete payment initiation response."));
            }

            return AppResponses.Success(
                "Stripe checkout session created.",
                new PaymentInitiationResponse(
                    "Stripe",
                    providerResponse.Id,
                    providerResponse.Url ?? string.Empty,
                    providerResponse.Status ?? "open"));
        }
        catch (Exception ex)
        {
            HttpClientLogDefinitions.LogExternalApiError(
                logger,
                "POST",
                _settings.CheckoutSessionsEndpoint,
                ex);

            return AppResponses.Failure<PaymentInitiationResponse>(
                AppError.DependencyUnavailable("Stripe payment provider is temporarily unavailable."));
        }
    }

    public async Task<AppResponse<PaymentStatusResponse>> GetStatusAsync(
        string paymentReference,
        string? provider = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentReference);

        if (!IsConfigured())
        {
            return AppResponses.Failure<PaymentStatusResponse>(
                AppError.DependencyUnavailable("Stripe payment provider is not configured for this environment."));
        }

        var statusEndpoint = $"{_settings.CheckoutSessionsEndpoint.TrimEnd('/')}/{Uri.EscapeDataString(paymentReference)}";

        try
        {
            using var httpClient = CreateClient(statusEndpoint);
            using var response = await httpClient
                .GetAsync(string.Empty, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                HttpClientLogDefinitions.LogExternalApiWarning(
                    logger,
                    "GET",
                    statusEndpoint,
                    (int)response.StatusCode);

                return AppResponses.Failure<PaymentStatusResponse>(
                    AppError.DependencyUnavailable("Stripe payment status could not be retrieved."));
            }

            using var stream = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            var providerResponse = await JsonSerializer
                .DeserializeAsync<StripePaymentStatusResponse>(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (providerResponse is null)
            {
                return AppResponses.Failure<PaymentStatusResponse>(
                    AppError.DependencyUnavailable("Stripe returned an incomplete payment status response."));
            }

            return AppResponses.Success(
                "Stripe payment status retrieved.",
                new PaymentStatusResponse(
                    "Stripe",
                    paymentReference,
                    providerResponse.NormalizedStatus,
                    providerResponse.NormalizedAmount,
                    providerResponse.NormalizedCurrency));
        }
        catch (Exception ex)
        {
            HttpClientLogDefinitions.LogExternalApiError(logger, "GET", statusEndpoint, ex);

            return AppResponses.Failure<PaymentStatusResponse>(
                AppError.DependencyUnavailable("Stripe payment provider is temporarily unavailable."));
        }
    }

    private HttpClient CreateClient(string? endpoint = null)
    {
        var httpClient = httpClientFactory.CreateClient("Payments.Stripe");
        httpClient.BaseAddress = new Uri(endpoint ?? _settings.CheckoutSessionsEndpoint);
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.SecretKey);

        return httpClient;
    }

    private bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(_settings.SecretKey) &&
        Uri.TryCreate(_settings.CheckoutSessionsEndpoint, UriKind.Absolute, out _);
}
