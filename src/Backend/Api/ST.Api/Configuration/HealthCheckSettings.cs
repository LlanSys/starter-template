namespace ST.Api.Configuration;

internal sealed class HealthCheckSettings
{
    public const string SectionName = "HealthChecks";

    public bool SqlServer { get; set; } = true;

    public bool Redis { get; set; } = true;

    public bool ProfileImageStorage { get; set; } = true;

    public bool KeyVault { get; set; }

    public string KeyVaultProbeSecretName { get; set; } = string.Empty;
}
