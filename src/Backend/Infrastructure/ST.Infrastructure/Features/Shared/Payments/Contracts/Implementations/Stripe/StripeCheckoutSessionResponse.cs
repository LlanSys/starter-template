using System.Text.Json.Serialization;

namespace ST.Infrastructure.Features.Shared.Payments.Contracts.Implementations.Stripe;

internal sealed record StripeCheckoutSessionResponse(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("status")] string? Status);
