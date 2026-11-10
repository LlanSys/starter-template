namespace ST.Infrastructure.Configuration;

public sealed class ExternalSignInSettings
{
    public const string SectionName = "ExternalSignIn";
    public string PublicApiOrigin { get; init; } = string.Empty;
    public string[] ClientOrigins { get; init; } = [];
}
