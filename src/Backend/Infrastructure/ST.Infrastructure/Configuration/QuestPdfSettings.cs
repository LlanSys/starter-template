namespace ST.Infrastructure.Configuration;

public sealed class QuestPdfSettings
{
    public bool Enabled { get; set; } = true;
    public string License { get; set; } = "Community";
}
