using System.IO;
using FluentAssertions;
using System;
using System.Linq;
using Xunit;
using ST.Persistence.Features.Shared.DataContext;

namespace ST.Tests.Architecture.Database;

public class TenantIsolationGuardrailTests
{
    [Fact]
    public void TenantFilteredDBContexts_MustHaveCorrespondingIntegrationTest()
    {
        var persistenceAssembly = typeof(SharedDBContext).Assembly;
        var tenantFilteredInterface = persistenceAssembly.GetTypes().FirstOrDefault(t => t.Name == "ITenantFilteredDBContext");

        tenantFilteredInterface.Should().NotBeNull("ITenantFilteredDBContext must exist in ST.Persistence.Common");

        var tenantContexts = persistenceAssembly.GetTypes()
            .Where(t => t.IsClass
                     && !t.IsAbstract
                     && tenantFilteredInterface!.IsAssignableFrom(t)
                     && !t.Name.Contains("SqlServer", StringComparison.OrdinalIgnoreCase)
                     && !t.Name.Contains("PostgreSql", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Path to the Integration Tests project
        var integrationTestsDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "tests", "ST.Tests.Integration");

        if (!Directory.Exists(integrationTestsDir))
        {
            var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (currentDir != null && currentDir.Name != "StarterTemplate")
            {
                currentDir = currentDir.Parent;
            }
            if (currentDir != null)
            {
                integrationTestsDir = Path.Combine(currentDir.FullName, "tests", "ST.Tests.Integration");
            }
        }

        Assert.True(Directory.Exists(integrationTestsDir), $"Could not find Integration Tests directory at {integrationTestsDir}");

        var testFiles = Directory.GetFiles(integrationTestsDir, "*TenantIsolationTests.cs", SearchOption.AllDirectories)
                                 .Select(Path.GetFileNameWithoutExtension)
                                 .ToList();

        foreach (var contextType in tenantContexts)
        {
            var expectedTestClassName = contextType.Name.Replace("DBContext", "") + "TenantIsolationTests";

            var hasTestFile = testFiles.Contains(expectedTestClassName);

            hasTestFile.Should().BeTrue(
                $"Because {contextType.Name} implements ITenantFilteredDBContext, there MUST be an integration test file named {expectedTestClassName}.cs to verify its tenant query filters are correctly isolated.");
        }
    }
}
