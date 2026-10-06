using ST.Application.Features.Shared.Payments.Contracts.Interfaces;
using ST.Domain.Features.Shared.Payments.Entities;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Features.Shared.Payments.Contracts.Implementations.Mpesa;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Payments.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ST.Infrastructure.Features.Shared.Payments.Contracts.Implementations;

internal sealed class MpesaPaymentGateway(
    IHttpClientFactory httpClientFactory,
    IOptions<PaymentSettings> options,
    ILogger<MpesaPaymentGateway> logger) : IPaymentGateway, IMpesaC2BService
{
    private readonly MpesaPaymentSettings _settings = options.Value.Mpesa;

    public async Task<AppResponse<PaymentInitiationResponse>> InitiateAsync(
        PaymentRecord record,
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsConfiguredForInitiation())
        {
            return AppResponses.Failure<PaymentInitiationResponse>(
                "M-Pesa payment provider is not configured for this environment.");
        }

        if (string.IsNullOrWhiteSpace(request.PayerPhoneNumber))
        {
            return AppResponses.Failure<PaymentInitiationResponse>(
                "Payer phone number is required for M-Pesa payments.");
        }

        try
        {
            var accessToken = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return AppResponses.Failure<PaymentInitiationResponse>(
                    "M-Pesa authentication failed. Please try again.");
            }

            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            var password = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_settings.ShortCode}{_settings.PassKey}{timestamp}"));

            var normalizedPhoneNumber = NormalizeMpesaPhone(request.PayerPhoneNumber);
            var accountRef = string.IsNullOrWhiteSpace(record.CustomerReference)
                ? _settings.AccountReference
                : record.CustomerReference;
            var transactionDesc = string.IsNullOrWhiteSpace(record.Description)
                ? "Payment"
                : record.Description;

            var providerRequest = new MpesaStkPushRequest(
                _settings.ShortCode,
                password,
                timestamp,
                "CustomerPayBillOnline",
                decimal.ToInt32(decimal.Round(record.Amount.Amount, 0, MidpointRounding.AwayFromZero)),
                normalizedPhoneNumber,
                _settings.ShortCode,
                normalizedPhoneNumber,
                string.IsNullOrWhiteSpace(_settings.CallbackUrlBase) ? request.CallbackUrl : $"{_settings.CallbackUrlBase.TrimEnd('/')}/api/v1/shared/payments/mobile-money/stk-callback",
                accountRef.Length > 12 ? accountRef[^12..] : accountRef,
                transactionDesc.Length > 13 ? transactionDesc[..13] : transactionDesc);

            using var httpClient = httpClientFactory.CreateClient("Payments.Mpesa");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient
                .PostAsJsonAsync(_settings.StkPushEndpoint, providerRequest, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                HttpClientLogDefinitions.LogExternalApiWarning(
                    logger,
                    "POST",
                    _settings.StkPushEndpoint,
                    (int)response.StatusCode);

                return AppResponses.Failure<PaymentInitiationResponse>(
                    $"M-Pesa rejected the request. Details: {errorBody}");
            }

            using var stream = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            var providerResponse = await JsonSerializer
                .DeserializeAsync<MpesaStkPushResponse>(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (providerResponse is null || string.IsNullOrWhiteSpace(providerResponse.CheckoutRequestId))
            {
                return AppResponses.Failure<PaymentInitiationResponse>(
                    "M-Pesa returned an incomplete payment initiation response.");
            }

            return AppResponses.Success(
                "M-Pesa STK push initiated.",
                new PaymentInitiationResponse(
                    "Mpesa",
                    providerResponse.CheckoutRequestId,
                    string.Empty,
                    providerResponse.ResponseDescription ?? "Pending"));
        }
        catch (Exception ex)
        {
            HttpClientLogDefinitions.LogExternalApiError(logger, "POST", _settings.StkPushEndpoint, ex);

            return AppResponses.Failure<PaymentInitiationResponse>(
                "M-Pesa payment provider is temporarily unavailable.");
        }
    }

    public async Task<AppResponse<PaymentStatusResponse>> GetStatusAsync(
        string paymentReference,
        string? provider = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentReference);

        if (!IsConfiguredForStatus())
        {
            return AppResponses.Failure<PaymentStatusResponse>(
                "M-Pesa payment status is not configured for this environment.");
        }

        try
        {
            var accessToken = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return AppResponses.Failure<PaymentStatusResponse>(
                    "M-Pesa authentication failed. Please try again.");
            }

            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            var password = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_settings.ShortCode}{_settings.PassKey}{timestamp}"));
            var providerRequest = new MpesaStkQueryRequest(
                _settings.ShortCode,
                password,
                timestamp,
                paymentReference);

            using var httpClient = httpClientFactory.CreateClient("Payments.Mpesa");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient
                .PostAsJsonAsync(_settings.StkQueryEndpoint, providerRequest, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                HttpClientLogDefinitions.LogExternalApiWarning(
                    logger,
                    "POST",
                    _settings.StkQueryEndpoint,
                    (int)response.StatusCode);

                return AppResponses.Failure<PaymentStatusResponse>(
                    "M-Pesa payment status could not be retrieved.");
            }

            using var stream = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            var providerResponse = await JsonSerializer
                .DeserializeAsync<MpesaStkQueryResponse>(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (providerResponse is null)
            {
                return AppResponses.Failure<PaymentStatusResponse>(
                    "M-Pesa returned an incomplete payment status response.");
            }

            return AppResponses.Success(
                "M-Pesa payment status retrieved.",
                new PaymentStatusResponse("Mpesa", paymentReference, providerResponse.NormalizedStatus, 0m, "KES"));
        }
        catch (Exception ex)
        {
            HttpClientLogDefinitions.LogExternalApiError(logger, "POST", _settings.StkQueryEndpoint, ex);

            return AppResponses.Failure<PaymentStatusResponse>(
                "M-Pesa payment provider is temporarily unavailable.");
        }
    }

    public async Task<AppResponse<string>> RegisterUrlsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfiguredForC2BRegistration())
        {
            return AppResponses.Failure<string>("M-Pesa C2B is not fully configured for URL registration.");
        }

        try
        {
            var accessToken = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return AppResponses.Failure<string>("M-Pesa authentication failed.");
            }

            var validationUrl = $"{_settings.CallbackUrlBase.TrimEnd('/')}/api/v1/shared/payments/mobile-money/c2b-validation";
            var confirmationUrl = $"{_settings.CallbackUrlBase.TrimEnd('/')}/api/v1/shared/payments/mobile-money/c2b-confirmation";

            var request = new MpesaC2BRegisterUrlRequest(
                _settings.C2BShortCode,
                "Completed",
                confirmationUrl,
                validationUrl);

            using var httpClient = httpClientFactory.CreateClient("Payments.Mpesa");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient
                .PostAsJsonAsync(_settings.C2BRegisterUrlEndpoint, request, cancellationToken)
                .ConfigureAwait(false);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                HttpClientLogDefinitions.LogExternalApiWarning(logger, "POST", _settings.C2BRegisterUrlEndpoint, (int)response.StatusCode);
                return AppResponses.Failure<string>($"URL Registration failed: {responseBody}");
            }

            return AppResponses.Success("C2B URLs registered successfully.", responseBody);
        }
        catch (Exception ex)
        {
            HttpClientLogDefinitions.LogExternalApiError(logger, "POST", _settings.C2BRegisterUrlEndpoint, ex);
            return AppResponses.Failure<string>("An error occurred during C2B URL registration.");
        }
    }

    public async Task<AppResponse<string>> SimulatePaymentAsync(decimal amount, string phoneNumber, string billRefNumber, CancellationToken cancellationToken = default)
    {
        if (!IsConfiguredForC2BSimulation())
        {
            return AppResponses.Failure<string>("M-Pesa C2B is not fully configured for simulation.");
        }

        try
        {
            var accessToken = await GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return AppResponses.Failure<string>("M-Pesa authentication failed.");
            }

            var request = new MpesaC2BSimulateRequest(
                _settings.C2BShortCode,
                "CustomerPayBillOnline",
                decimal.ToInt32(decimal.Round(amount, 0, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture),
                NormalizeMpesaPhone(phoneNumber),
                billRefNumber);

            using var httpClient = httpClientFactory.CreateClient("Payments.Mpesa");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient
                .PostAsJsonAsync(_settings.C2BSimulateEndpoint, request, cancellationToken)
                .ConfigureAwait(false);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                HttpClientLogDefinitions.LogExternalApiWarning(logger, "POST", _settings.C2BSimulateEndpoint, (int)response.StatusCode);
                return AppResponses.Failure<string>($"Payment simulation failed: {responseBody}");
            }

            return AppResponses.Success("C2B payment simulated successfully.", responseBody);
        }
        catch (Exception ex)
        {
            HttpClientLogDefinitions.LogExternalApiError(logger, "POST", _settings.C2BSimulateEndpoint, ex);
            return AppResponses.Failure<string>("An error occurred during C2B payment simulation.");
        }
    }

    private async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        using var httpClient = httpClientFactory.CreateClient("Payments.Mpesa.Auth");
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_settings.ConsumerKey}:{_settings.ConsumerSecret}"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        using var response = await httpClient
            .GetAsync(_settings.AuthEndpoint, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            HttpClientLogDefinitions.LogExternalApiWarning(
                logger,
                "GET",
                _settings.AuthEndpoint,
                (int)response.StatusCode);

            return null;
        }

        using var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        var providerResponse = await JsonSerializer
            .DeserializeAsync<MpesaAccessTokenResponse>(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return providerResponse?.AccessToken;
    }

    private bool IsConfiguredForInitiation() =>
        IsAuthConfigured() &&
        !string.IsNullOrWhiteSpace(_settings.ShortCode) &&
        !string.IsNullOrWhiteSpace(_settings.PassKey) &&
        !string.IsNullOrWhiteSpace(_settings.StkPushEndpoint);

    private bool IsConfiguredForStatus() =>
        IsConfiguredForInitiation() &&
        !string.IsNullOrWhiteSpace(_settings.StkQueryEndpoint);

    private bool IsAuthConfigured() =>
        !string.IsNullOrWhiteSpace(_settings.ConsumerKey) &&
        !string.IsNullOrWhiteSpace(_settings.ConsumerSecret) &&
        !string.IsNullOrWhiteSpace(_settings.AuthEndpoint) &&
        !string.IsNullOrWhiteSpace(_settings.BaseUrl);

    private bool IsConfiguredForC2BRegistration() =>
        IsAuthConfigured() &&
        !string.IsNullOrWhiteSpace(_settings.C2BShortCode) &&
        !string.IsNullOrWhiteSpace(_settings.C2BRegisterUrlEndpoint) &&
        !string.IsNullOrWhiteSpace(_settings.CallbackUrlBase);

    private bool IsConfiguredForC2BSimulation() =>
        IsAuthConfigured() &&
        !string.IsNullOrWhiteSpace(_settings.C2BShortCode) &&
        !string.IsNullOrWhiteSpace(_settings.C2BSimulateEndpoint);

    private static string NormalizeMpesaPhone(string phoneNumber)
    {
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        return digits.StartsWith('0')
            ? $"254{digits[1..]}"
            : digits;
    }
}
