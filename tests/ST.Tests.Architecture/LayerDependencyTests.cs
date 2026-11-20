using System.Reflection;
using FluentAssertions;

namespace ST.Tests.Architecture;

public sealed class LayerDependencyTests
{
    private static readonly Assembly[] PlatformAssemblies =
    [
        AssemblyReferences.Domain, AssemblyReferences.SharedKernel, AssemblyReferences.SharedKernelValidation,
        AssemblyReferences.Application, AssemblyReferences.Persistence, AssemblyReferences.Infrastructure,
        AssemblyReferences.Api, AssemblyReferences.Blazor
    ];

    public static TheoryData<string, string[]> CompiledDependencyLimits => new()
    {
        { "ST.Domain", [] },
        { "ST.SharedKernel", [] },
        { "ST.SharedKernel.Validation", ["ST.Domain", "ST.SharedKernel"] },
        { "ST.Application", ["ST.Domain", "ST.SharedKernel", "ST.SharedKernel.Validation"] },
        { "ST.Persistence", ["ST.Domain"] },
        {
            "ST.Infrastructure",
            ["ST.Application", "ST.Persistence", "ST.Domain", "ST.SharedKernel"]
        },
        {
            "ST.Api",
            ["ST.Application", "ST.Infrastructure", "ST.Persistence", "ST.Domain", "ST.SharedKernel"]
        },
        { "ST.UI.Blazor", ["ST.SharedKernel"] }
    };

    [Theory]
    [MemberData(nameof(CompiledDependencyLimits))]
    public void Assembly_Uses_Only_Approved_Solution_Projects(string assemblyName, string[] approvedReferences)
    {
        var assembly = PlatformAssemblies.Single(candidate => candidate.GetName().Name == assemblyName);
        var projectNames = Directory.EnumerateFiles(
                Path.Combine(AssemblyReferences.RepoRoot, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet(StringComparer.Ordinal);

        var forbiddenReferences = assembly.GetReferencedAssemblies()
            .Where(reference => projectNames.Contains(reference.Name))
            .Select(reference => reference.Name)
            .Except(approvedReferences, StringComparer.Ordinal)
            .ToArray();

        forbiddenReferences.Should().BeEmpty(
            because: "{0} must stay inside its compiled dependency boundary", assemblyName);
    }

    [Fact]
    public void Blazor_Does_Not_Use_Backend_Execution_Libraries()
    {
        var forbiddenReferences = AssemblyReferences.Blazor.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name =>
                name.Equals("MediatR", StringComparison.Ordinal) ||
                name.StartsWith("MediatR.", StringComparison.Ordinal) ||
                name.Equals("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ||
                name.StartsWith("Microsoft.EntityFrameworkCore.", StringComparison.Ordinal))
            .ToArray();

        forbiddenReferences.Should().BeEmpty(because: "Blazor uses typed HTTP clients for backend operations");
    }
}
