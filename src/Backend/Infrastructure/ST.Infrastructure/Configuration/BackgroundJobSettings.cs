namespace ST.Infrastructure.Configuration;

public sealed class BackgroundJobSettings
{
    public const string SectionName = "BackgroundJobs";

    public bool Enabled { get; set; } = true;
    public Guid LegacyTenantId { get; init; }
}
