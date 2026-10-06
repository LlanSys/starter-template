namespace ST.Api.Configuration;

internal sealed class DataProtectionSettings
{
    public const string SectionName = "DataProtection";

    public string ApplicationName { get; init; } = "StarterTemplate";
    public string? KeysPath { get; init; }
    public string? BlobKeyUri { get; init; }
    public string? RedisKeyRingConnectionString { get; init; }
    public string RedisKeyRingKey { get; init; } = "DataProtection-Keys";
    public string? KeyVaultKeyIdentifier { get; init; }
    public string? CertificateThumbprint { get; init; }
    public string KeyEncryptionMode { get; init; } = "Auto";
    public bool UseExternalKeyStore { get; init; } = true;
}
