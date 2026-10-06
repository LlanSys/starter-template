using System.Text.Json.Serialization;

namespace ST.Infrastructure.Features.Shared.Payments.Contracts.Implementations.Mpesa;

internal sealed record MpesaAccessTokenResponse(
    [property: JsonPropertyName("access_token")] string? AccessToken);
