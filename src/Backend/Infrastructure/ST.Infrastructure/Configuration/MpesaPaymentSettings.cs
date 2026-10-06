namespace ST.Infrastructure.Configuration;

public sealed class MpesaPaymentSettings
{
    public string Environment { get; set; } = "Sandbox"; // "Sandbox" or "Live"
    public string ConsumerKey { get; set; } = string.Empty;
    public string ConsumerSecret { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty; // For STK Push
    public string C2BShortCode { get; set; } = string.Empty; // For C2B
    public string PassKey { get; set; } = string.Empty;
    public string AccountReference { get; set; } = "StarterTemplate";

    // Explicit endpoints (can be derived from Environment, but good for overriding)
    public string BaseUrl => Environment.Equals("Live", StringComparison.OrdinalIgnoreCase)
        ? "https://api.safaricom.co.ke"
        : "https://sandbox.safaricom.co.ke";

    public string AuthEndpoint { get; set; } = "/oauth/v1/generate?grant_type=client_credentials";
    public string StkPushEndpoint { get; set; } = "/mpesa/stkpush/v1/processrequest";
    public string StkQueryEndpoint { get; set; } = "/mpesa/stkpushquery/v1/query";
    public string C2BRegisterUrlEndpoint { get; set; } = "/mpesa/c2b/v1/registerurl";
    public string C2BSimulateEndpoint { get; set; } = "/mpesa/c2b/v1/simulate";
    public string CallbackUrlBase { get; set; } = string.Empty;
}
