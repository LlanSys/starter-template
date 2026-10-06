namespace ST.Infrastructure.Features.Shared.Payments.Contracts.Implementations;

internal static class PaymentProviderParser
{
    public static PaymentProviderKind Parse(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return PaymentProviderKind.NoOp;
        }

        return Normalize(provider) switch
        {
            "NOOP" or "NONE" => PaymentProviderKind.NoOp,
            "STRIPE" or "CARD" or "CARDPAYMENT" => PaymentProviderKind.Stripe,
            "MPESA" or "MPESASTK" => PaymentProviderKind.Mpesa,
            _ => PaymentProviderKind.Invalid
        };
    }

    private static string Normalize(string provider) =>
        provider
            .Trim()
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();
}
