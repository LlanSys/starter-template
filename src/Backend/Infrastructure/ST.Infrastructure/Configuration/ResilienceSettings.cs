namespace ST.Infrastructure.Configuration;

public sealed class ResilienceSettings
{
    public const string SectionName = "Resilience";

    public bool Enabled { get; set; } = true;
}
