namespace ST.Infrastructure.Configuration;

public sealed class GoogleSsoSettings
{
    public const string SectionName = "GoogleSso";
    public bool Enabled { get; init; }
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string CallbackPath { get; init; } = "/signin-google";
}
