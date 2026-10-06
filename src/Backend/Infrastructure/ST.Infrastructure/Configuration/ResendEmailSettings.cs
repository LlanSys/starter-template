namespace ST.Infrastructure.Configuration;

public sealed class ResendEmailSettings
{
    public string Endpoint { get; set; } = "https://api.resend.com/emails";
    public string ApiKey { get; set; } = string.Empty;
}
