using System.Globalization;

namespace ST.Infrastructure.Features.Shared.Payments.Contracts.Implementations.Stripe;

internal sealed record StripeCheckoutSessionRequest(
    string Mode,
    string SuccessUrl,
    string CancelUrl,
    string ClientReferenceId,
    string Currency,
    string UnitAmount,
    string ProductName)
{
    public static StripeCheckoutSessionRequest From(
        decimal amount,
        string currency,
        string description,
        string customerReference,
        string callbackUrl,
        string? configuredSuccessUrl,
        string? configuredCancelUrl)
    {
        var unitAmount = decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero)
            .ToString("0", CultureInfo.InvariantCulture);
        var successUrl = string.IsNullOrWhiteSpace(configuredSuccessUrl)
            ? callbackUrl
            : configuredSuccessUrl;
        var cancelUrl = string.IsNullOrWhiteSpace(configuredCancelUrl)
            ? callbackUrl
            : configuredCancelUrl;

        return new StripeCheckoutSessionRequest(
            "payment",
            successUrl,
            cancelUrl,
            customerReference,
            currency.ToLowerInvariant(),
            unitAmount,
            description);
    }

    public Dictionary<string, string> ToFormFields() => new()
    {
        ["mode"] = Mode,
        ["success_url"] = SuccessUrl,
        ["cancel_url"] = CancelUrl,
        ["client_reference_id"] = ClientReferenceId,
        ["line_items[0][quantity]"] = "1",
        ["line_items[0][price_data][currency]"] = Currency,
        ["line_items[0][price_data][unit_amount]"] = UnitAmount,
        ["line_items[0][price_data][product_data][name]"] = ProductName
    };
}
