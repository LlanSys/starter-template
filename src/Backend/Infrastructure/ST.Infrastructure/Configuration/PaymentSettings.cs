namespace ST.Infrastructure.Configuration;

public sealed class PaymentSettings
{
    public const string SectionName = "Payments";

    public string Provider { get; set; } = "NoOp";
    public bool AllowNoOpInProduction { get; set; }
    public StripePaymentSettings Stripe { get; set; } = new();
    public MpesaPaymentSettings Mpesa { get; set; } = new();
}
