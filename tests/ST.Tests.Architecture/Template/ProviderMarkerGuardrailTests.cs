using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace ST.Tests.Architecture.Template;

/// <summary>
/// Template-only guardrail (removed from single-provider forks): provider-specific API usage in shared
/// code must sit inside a matching <c>db:</c> marker block so scripts/select-database-provider.ps1 can
/// remove it. Whole provider files are covered by the 'paths' globs in scripts/database-providers.json.
/// See docs/architecture/database-provider-selection.md.
/// </summary>
[Trait("Category", "Guardrail")]
public sealed partial class ProviderMarkerGuardrailTests
{
    private static readonly string[] ScannedRoots = ["src", "tests", "tools"];
    private static readonly string[] SkippedDirectories = ["bin", "obj", ".git", "node_modules", "artifacts", "Migrations"];

    private static readonly (string Provider, Regex Pattern)[] ProviderTokens =
    [
        ("SqlServer", new Regex(@"UseSqlServer|IsSqlServer|SqlException|SqlConnection|Microsoft\.Data\.SqlClient|Testcontainers\.MsSql", RegexOptions.Compiled)),
        ("PostgreSql", new Regex(@"UseNpgsql|IsNpgsql|UsePostgres|PostgresException|NpgsqlConnection|using Npgsql|Testcontainers\.PostgreSql", RegexOptions.Compiled)),
    ];

    [GeneratedRegex(@"^\s*(?://|#|--|REM\b|<!--)\s*<(?<close>/)?db:(?<provider>[A-Za-z]+)>\s*(?:-->)?\s*$")]
    private static partial Regex MarkerPattern();

    [Fact]
    public void ProviderSpecificCodeIsMarkedOrInProviderFiles()
    {
        var root = FindRepositoryRoot();
        var manifest = LoadManifest(root);
        var providerPaths = manifest.Paths.Select(GlobToRegex).ToArray();
        var violations = new List<string>();

        foreach (var file in EnumerateSourceFiles(root))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            if (relative.StartsWith("tests/ST.Tests.Architecture/Template/", StringComparison.Ordinal)
                || providerPaths.Any(pattern => pattern.IsMatch(relative)))
            {
                continue;
            }

            string? open = null;
            var openLine = 0;
            var lines = File.ReadAllLines(file);
            for (var index = 0; index < lines.Length; index++)
            {
                var number = index + 1;
                var marker = MarkerPattern().Match(lines[index]);
                if (marker.Success)
                {
                    var provider = marker.Groups["provider"].Value;
                    if (!manifest.Providers.Contains(provider))
                    {
                        violations.Add($"{relative}:{number}: unknown provider marker '{provider}'.");
                    }
                    else if (marker.Groups["close"].Success)
                    {
                        if (open != provider)
                        {
                            violations.Add($"{relative}:{number}: closing '{provider}' marker does not match open block '{open}'.");
                        }

                        open = null;
                    }
                    else
                    {
                        if (open is not null)
                        {
                            violations.Add($"{relative}:{number}: '{provider}' marker nested inside '{open}' block from line {openLine}.");
                        }

                        open = provider;
                        openLine = number;
                    }

                    continue;
                }

                foreach (var (provider, pattern) in ProviderTokens)
                {
                    if (pattern.IsMatch(lines[index]) && open != provider)
                    {
                        violations.Add($"{relative}:{number}: {provider} API outside a <db:{provider}> block: {lines[index].Trim()}");
                    }
                }
            }

            if (open is not null)
            {
                violations.Add($"{relative}:{openLine}: '{open}' marker is never closed.");
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> EnumerateSourceFiles(string root)
    {
        foreach (var scanned in ScannedRoots)
        {
            var directory = Path.Combine(root, scanned);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            var pending = new Stack<string>([directory]);
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                foreach (var child in Directory.EnumerateDirectories(current))
                {
                    if (!SkippedDirectories.Contains(Path.GetFileName(child), StringComparer.OrdinalIgnoreCase))
                    {
                        pending.Push(child);
                    }
                }

                foreach (var file in Directory.EnumerateFiles(current))
                {
                    if (file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                        || file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return file;
                    }
                }
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root (AGENTS.md) was not found.");
    }

    private static (HashSet<string> Providers, List<string> Paths) LoadManifest(string root)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "scripts", "database-providers.json")));
        var providers = new HashSet<string>(StringComparer.Ordinal);
        var paths = new List<string>();
        foreach (var provider in document.RootElement.GetProperty("providers").EnumerateObject())
        {
            providers.Add(provider.Name);
            paths.AddRange(provider.Value.GetProperty("paths").EnumerateArray().Select(path => path.GetString()!));
        }

        return (providers, paths);
    }

    // Mirrors ConvertTo-TemplatePathRegex in scripts/lib/TemplateFiles.ps1.
    private static Regex GlobToRegex(string glob)
    {
        var pattern = Regex.Escape(glob.Replace('\\', '/'))
            .Replace(@"\*\*/", "(?:.*/)?", StringComparison.Ordinal)
            .Replace(@"\*\*", ".*", StringComparison.Ordinal)
            .Replace(@"\*", "[^/]*", StringComparison.Ordinal)
            .Replace(@"\?", "[^/]", StringComparison.Ordinal);
        return new Regex(glob.EndsWith('/') ? "^" + pattern + ".*$" : "^" + pattern + "$", RegexOptions.CultureInvariant);
    }
}
