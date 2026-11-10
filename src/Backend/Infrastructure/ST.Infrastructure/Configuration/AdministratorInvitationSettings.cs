namespace ST.Infrastructure.Configuration;

internal sealed class AdministratorInvitationSettings
{
    public const string SectionName = "AdministratorInvitations";

    public int ValidityHours { get; init; } = 48;
}
