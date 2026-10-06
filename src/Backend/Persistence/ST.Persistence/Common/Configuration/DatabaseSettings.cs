namespace ST.Persistence.Common.Configuration;

public class DatabaseSettings
{
    public const string SectionName = "DatabaseSettings";

    public string Provider { get; set; } = "SqlServer";
}
