namespace ST.Infrastructure.Configuration;

public class SmsSettings
{
    public const string SectionName = "SmsSettings";

    public TwilioSettings Twilio { get; set; } = new();

    public bool EnableProviderFallback { get; set; } = true;
}
