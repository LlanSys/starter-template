using System.Text.Json;
using FluentAssertions;

namespace ST.Tests.Architecture;

public sealed class ConfigurationIdentityTests
{
    private static readonly string[] AppSettingsFiles =
    [
        "appsettings.json",
        "appsettings.Development.json",
        "appsettings.Production.json"
    ];

    [Fact]
    public void DataProtectionApplicationName_Should_Be_Consistent_Across_Environments()
    {
        var apiDirectory = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Backend",
            "Api",
            "ST.Api");

        var applicationNames = AppSettingsFiles
            .Select(fileName => ReadDataProtectionApplicationName(Path.Combine(apiDirectory, fileName)))
            .ToArray();

        applicationNames.Should().OnlyContain(
            applicationName => applicationName == "StarterTemplate",
            "the unrenamed template must use one stable Data Protection discriminator in every environment");
    }

    private static string ReadDataProtectionApplicationName(string path)
    {
        var options = new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        };

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement
            .GetProperty("DataProtection")
            .GetProperty("ApplicationName")
            .GetString() ?? string.Empty;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the repository root containing AGENTS.md.");
    }
}
