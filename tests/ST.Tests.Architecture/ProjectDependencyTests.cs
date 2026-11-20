using System.Xml.Linq;
using FluentAssertions;

namespace ST.Tests.Architecture;

public sealed class ProjectDependencyTests
{
    public static TheoryData<string, string[]> DirectProjectGraph => new()
    {
        { "Backend/Domain/ST.Domain", [] },
        { "Shared/ST.SharedKernel", [] },
        { "Shared/ST.SharedKernel.Validation", ["Backend/Domain/ST.Domain", "Shared/ST.SharedKernel"] },
        {
            "Backend/Application/ST.Application",
            ["Backend/Domain/ST.Domain", "Shared/ST.SharedKernel", "Shared/ST.SharedKernel.Validation"]
        },
        { "Backend/Persistence/ST.Persistence", ["Backend/Domain/ST.Domain"] },
        {
            "Backend/Infrastructure/ST.Infrastructure",
            ["Backend/Application/ST.Application", "Backend/Persistence/ST.Persistence"]
        },
        {
            "Backend/Api/ST.Api",
            ["Backend/Application/ST.Application", "Backend/Infrastructure/ST.Infrastructure",
                "Backend/Persistence/ST.Persistence"]
        },
        { "Frontend/Web/ST.UI.Blazor", ["Shared/ST.SharedKernel"] }
    };

    [Theory]
    [MemberData(nameof(DirectProjectGraph))]
    public void Project_Declares_Only_Its_Approved_Direct_References(string project, string[] approvedReferences)
    {
        var projectFile = GetProjectFile(project);
        var projectDirectory = Path.GetDirectoryName(projectFile)!;
        var references = XDocument.Load(projectFile)
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => (string?)element.Attribute("Include"))
            .Where(include => include is not null)
            .SelectMany(include => include!.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(include => Path.GetFullPath(include.Replace('\\', '/'), projectDirectory))
            .ToArray();

        references.Should().BeEquivalentTo(
            approvedReferences.Select(GetProjectFile),
            because: "{0} has an explicit direct project dependency boundary", project);
    }

    private static string GetProjectFile(string project)
    {
        return Path.GetFullPath(Path.Combine(
            AssemblyReferences.RepoRoot, "src", project, Path.GetFileName(project) + ".csproj"));
    }
}
