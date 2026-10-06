using ST.Application.Features.Shared.Payments.Contracts.Interfaces;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Payments.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ST.Infrastructure.Features.Shared.Payments.Contracts.Implementations;

internal sealed class StripeWebhookVerifier(
    IOptions<PaymentSettings> options,
    ILogger<StripeWebhookVerifier> logger) : IPaymentWebhookVerifier
{
    private static readonly TimeSpan SignatureTolerance = TimeSpan.FromMinutes(5);
    private readonly StripePaymentSettings _settings = options.Value.Stripe;

    public Task<AppResponse<PaymentWebhookVerificationResponse>> VerifyAsync(
        string provider,
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsStripe(provider))
        {
            return Task.FromResult(AppResponses.Failure<PaymentWebhookVerificationResponse>(
                AppError.BusinessRule("The payment webhook provider is not supported.")));
        }

        if (string.IsNullOrWhiteSpace(_settings.WebhookSigningSecret))
        {
            ServiceLogDefinitions.LogPaymentWebhookVerificationFailed(logger, "Stripe", "Webhook signing secret is not configured.");
            return Task.FromResult(AppResponses.Failure<PaymentWebhookVerificationResponse>(
                AppError.DependencyUnavailable("Stripe webhook verification is not configured.")));
        }

        if (!IsSignatureValid(payload, signatureHeader, _settings.WebhookSigningSecret, out var reason))
        {
            ServiceLogDefinitions.LogPaymentWebhookVerificationFailed(logger, "Stripe", reason);
            return Task.FromResult(AppResponses.Failure<PaymentWebhookVerificationResponse>(
                AppError.Forbidden("Stripe webhook signature verification failed.")));
        }

        try
        {
            var verification = ExtractWebhookResponse(payload);
            ServiceLogDefinitions.LogPaymentWebhookVerified(logger, verification.Provider, verification.EventId, verification.EventType);
            return Task.FromResult(AppResponses.Success("Stripe webhook verified.", verification));
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            ServiceLogDefinitions.LogPaymentWebhookPayloadError(logger, "Stripe", ex);
            return Task.FromResult(AppResponses.Failure<PaymentWebhookVerificationResponse>(
                AppError.BusinessRule("Stripe webhook payload could not be processed.")));
        }
    }

    private static bool IsStripe(string provider) =>
        provider.Equals("Stripe", StringComparison.OrdinalIgnoreCase);

    private static bool IsSignatureValid(
        string payload,
        string signatureHeader,
        string signingSecret,
        out string reason)
    {
        reason = string.Empty;

        var timestamp = GetHeaderValue(signatureHeader, "t");
        var signature = GetHeaderValue(signatureHeader, "v1");
        if (string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(signature))
        {
            reason = "Missing Stripe timestamp or v1 signature.";
            return false;
        }

        if (!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var unixTimestamp))
        {
            reason = "Invalid Stripe timestamp.";
            return false;
        }

        var signedAt = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        var difference = DateTimeOffset.UtcNow - signedAt;
        if (difference > SignatureTolerance || difference < -SignatureTolerance)
        {
            reason = "Stripe signature timestamp is outside tolerance.";
            return false;
        }

        var expectedSignature = ComputeSignature($"{timestamp}.{payload}", signingSecret);
        if (CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(expectedSignature),
                Encoding.ASCII.GetBytes(signature)))
        {
            return true;
        }

        reason = "Stripe v1 signature mismatch.";
        return false;
    }

    private static string ComputeSignature(string payload, string signingSecret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(signingSecret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(keyBytes);
        return Convert.ToHexString(hmac.ComputeHash(payloadBytes)).ToLowerInvariant();
    }

    private static string? GetHeaderValue(string header, string key)
    {
        var prefix = $"{key}=";
        return header
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(part => part.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))?[prefix.Length..];
    }

    private static PaymentWebhookVerificationResponse ExtractWebhookResponse(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var eventId = root.GetProperty("id").GetString() ?? string.Empty;
        var eventType = root.GetProperty("type").GetString() ?? string.Empty;
        var paymentObject = root.GetProperty("data").GetProperty("object");
        var paymentReference = paymentObject.GetProperty("id").GetString() ?? string.Empty;
        var customerReference = GetOptionalString(paymentObject, "client_reference_id") ?? string.Empty;
        var status = GetOptionalString(paymentObject, "payment_status")
            ?? GetOptionalString(paymentObject, "status")
            ?? "unknown";

        return new PaymentWebhookVerificationResponse(
            "Stripe",
            eventId,
            eventType,
            customerReference,
            paymentReference,
            status);
    }

    private static string? GetOptionalString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) ? property.GetString() : null;
}
