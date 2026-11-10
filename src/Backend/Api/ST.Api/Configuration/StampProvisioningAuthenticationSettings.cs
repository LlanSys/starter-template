namespace ST.Api.Configuration;

internal sealed class StampProvisioningAuthenticationSettings
{
    public const string SectionName = "ControlPlane:Provisioning:Authentication";

    public bool Enabled { get; init; }
    public string Audience { get; init; } = "urn:startertemplate:provisioning";
    public string Repository { get; init; } = string.Empty;
    public string RepositoryId { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Ref { get; init; } = "refs/heads/main";
}
