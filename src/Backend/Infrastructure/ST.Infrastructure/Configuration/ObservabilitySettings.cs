namespace ST.Infrastructure.Configuration;

public sealed class ObservabilitySettings
{
    public const string SectionName = "Observability";

    public bool Enabled { get; set; } = true;
    public string ServiceName { get; set; } = "StarterTemplate.API";
    public string ServiceNamespace { get; set; } = "StarterTemplate";
    public AzureMonitorSettings AzureMonitor { get; set; } = new();

    public OtlpSettings Otlp { get; set; } = new();
}
