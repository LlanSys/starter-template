namespace ST.Infrastructure.Configuration;

public sealed class AuthProviderSettings
{
    public const string SectionName = "AuthProvider";

    public string Provider { get; set; } = "AspNetCoreIdentity";
    public bool Enabled { get; set; } = true;
}
