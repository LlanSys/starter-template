using FluentAssertions;

namespace ST.Tests.Architecture;

public sealed class AgentInstructionTests
{
    private static readonly string[] WrapperFiles =
    [
        ".github/copilot-instructions.md",
        "CLAUDE.md",
        "GEMINI.md",
        ".cursor/rules/starter-template.mdc",
        ".windsurfrules"
    ];

    [Fact]
    public void Repository_Should_Have_Canonical_Agent_Instructions()
    {
        var root = FindRepositoryRoot();
        var agentsPath = Path.Combine(root, "AGENTS.md");

        File.Exists(agentsPath).Should().BeTrue("AI coding agents need a canonical repo instruction file");

        var content = File.ReadAllText(agentsPath);
        content.Should().Contain("canonical source of truth");
        content.Should().Contain("Start Every Coding Or Debugging Task");
        content.Should().Contain("If a convention changes, update this file");
    }

    [Fact]
    public void Tool_Specific_Instruction_Files_Should_Point_To_Agents_File()
    {
        var root = FindRepositoryRoot();

        foreach (var wrapperFile in WrapperFiles)
        {
            var path = Path.Combine(root, wrapperFile.Replace('/', Path.DirectorySeparatorChar));

            File.Exists(path).Should().BeTrue($"{wrapperFile} should exist and point tools back to AGENTS.md");

            var content = File.ReadAllText(path);
            content.Should().Contain("AGENTS.md", $"{wrapperFile} must not drift into an independent rulebook");
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "StarterTemplate.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
    }
}
