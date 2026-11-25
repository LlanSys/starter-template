namespace ST.Domain.Shared;

/// <summary>
/// Database provider names supported by this build. Each provider is wrapped in a
/// <c>db:</c> marker block so fork-time selection (scripts/select-database-provider.ps1)
/// keeps only the chosen provider. See docs/architecture/database-provider-selection.md.
/// </summary>
public static class DatabaseProviders
{
    // <db:SqlServer>
    public const string SqlServer = "SqlServer";
    // </db:SqlServer>
    // <db:PostgreSql>
    public const string PostgreSql = "PostgreSql";
    // </db:PostgreSql>

    public static IReadOnlyList<string> Supported { get; } =
    [
        // <db:SqlServer>
        SqlServer,
        // </db:SqlServer>
        // <db:PostgreSql>
        PostgreSql,
        // </db:PostgreSql>
    ];

    public static bool Is(string? provider, string expected) =>
        string.Equals(provider, expected, StringComparison.OrdinalIgnoreCase);

    public static bool IsSupported(string? provider) =>
        provider is not null && Supported.Any(candidate => Is(provider, candidate));

    /// <summary>Returns the canonical provider name, or throws when this build does not include it.</summary>
    public static string Normalize(string? provider) =>
        Supported.FirstOrDefault(candidate => Is(provider, candidate))
        ?? throw new InvalidOperationException(
            $"Database provider '{provider}' is not supported by this build. Supported: {string.Join(", ", Supported)}.");
}
