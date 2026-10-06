namespace ST.Infrastructure.Configuration;

public sealed class SendGridEmailSettings
{
    public string Endpoint { get; set; } = "https://api.sendgrid.com/v3/mail/send";
    public string ApiKey { get; set; } = string.Empty;
}
