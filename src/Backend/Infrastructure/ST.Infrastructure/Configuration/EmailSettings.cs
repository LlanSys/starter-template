namespace ST.Infrastructure.Configuration;

public sealed class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string Provider { get; set; } = "NoOp";
    public string FromAddress { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ClientBaseUrl { get; set; } = string.Empty;
    public string TemplatePath { get; set; } = string.Empty;

    public bool AllowNoOpInProduction { get; set; }

    public LocalMailpitEmailSettings LocalMailpit { get; set; } = new();
    public SendGridEmailSettings SendGrid { get; set; } = new();
    public AzureCommunicationEmailSettings AzureCommunication { get; set; } = new();
    public ResendEmailSettings Resend { get; set; } = new();
}
