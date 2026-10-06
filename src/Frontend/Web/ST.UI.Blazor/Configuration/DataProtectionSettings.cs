namespace ST.UI.Blazor.Configuration;

internal sealed class DataProtectionSettings
{
    public const string SectionName = "DataProtection";

    public string ApplicationName { get; init; } = "StarterTemplate";
    public string? KeysPath { get; init; }
}
