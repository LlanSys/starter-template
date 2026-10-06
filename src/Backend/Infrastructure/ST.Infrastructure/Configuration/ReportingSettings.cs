namespace ST.Infrastructure.Configuration;

public sealed class ReportingSettings
{
    public const string SectionName = "Reporting";

    public QuestPdfSettings QuestPdf { get; set; } = new();
}
