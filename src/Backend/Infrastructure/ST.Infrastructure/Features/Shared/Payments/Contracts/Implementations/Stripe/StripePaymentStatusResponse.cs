using System.Text.Json.Serialization;

namespace ST.Infrastructure.Features.Shared.Payments.Contracts.Implementations.Stripe;

internal sealed record StripePaymentStatusResponse(
    [property: JsonPropertyName("payment_status")] string? PaymentStatus,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("amount_total")] long? AmountTotal,
    [property: JsonPropertyName("currency")] string? Currency)
{
    public string NormalizedStatus => PaymentStatus ?? Status ?? "unknown";

    public decimal NormalizedAmount => (AmountTotal ?? 0L) / 100m;

    public string NormalizedCurrency => Currency?.ToUpperInvariant() ?? string.Empty;
}
