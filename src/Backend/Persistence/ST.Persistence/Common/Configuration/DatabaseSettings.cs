using ST.Domain.Shared;

namespace ST.Persistence.Common.Configuration;

public class DatabaseSettings
{
    public const string SectionName = "DatabaseSettings";

    // Fork-time provider selection rewrites this default (scripts/database-providers.json).
    public const string DefaultProvider = "SqlServer";

    public string Provider { get; set; } = DefaultProvider;

    public bool Is(string provider) => DatabaseProviders.Is(Provider, provider);

    /// <summary>Fails fast when configuration names a provider this build no longer contains.</summary>
    public DatabaseSettings EnsureSupported()
    {
        Provider = DatabaseProviders.Normalize(Provider);
        return this;
    }
}
