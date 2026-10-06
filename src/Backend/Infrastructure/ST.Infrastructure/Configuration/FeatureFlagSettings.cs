namespace ST.Infrastructure.Configuration;

public sealed class FeatureFlagSettings
{
    public const string SectionName = "FeatureFlags";

    public string Provider { get; set; } = "Configuration";
    public bool FailClosed { get; set; } = true;
    public Dictionary<string, bool> Flags { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
