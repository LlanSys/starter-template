namespace ST.Infrastructure.Configuration;

public sealed class MfaSettings
{
    public const string SectionName = "SecuritySettings:Mfa";

    public bool Enabled { get; init; } = true;

    public bool EnforceEnrollment { get; init; } = true;

    public string[] RequiredRoles { get; init; } = ["System Administrator"];
}
