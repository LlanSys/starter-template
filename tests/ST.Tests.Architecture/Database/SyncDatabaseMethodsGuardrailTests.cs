using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using System.Linq;
using Xunit;

namespace ST.Tests.Architecture.Database;

public class SyncDatabaseMethodsGuardrailTests
{
    [Fact]
    public void SourceCode_ShouldNotCall_SynchronousDatabaseMethods()
    {
        // Define the synchronous methods we want to forbid
        var forbiddenMethods = new[] { "Migrate", "OpenConnection", "CanConnect" };

        // Path to the Backend source code
        var backendDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "src", "Backend");

        // As a fallback for tests run from different directories (e.g., CI vs local VS test runner)
        if (!Directory.Exists(backendDir))
        {
            var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (currentDir != null && currentDir.Name != "StarterTemplate")
            {
                currentDir = currentDir.Parent;
            }
            if (currentDir != null)
            {
                backendDir = Path.Combine(currentDir.FullName, "src", "Backend");
            }
        }

        Assert.True(Directory.Exists(backendDir), $"Could not find Backend directory at {backendDir}");

        var csharpFiles = Directory.GetFiles(backendDir, "*.cs", SearchOption.AllDirectories)
                                   // Exclude migrations because they are generated and outside of runtime paths.
                                   .Where(f => !f.Contains("Migrations"))
                                   .ToList();

        var offendingFiles = Enumerable.Empty<string>().ToList();

        foreach (var file in csharpFiles)
        {
            var code = File.ReadAllText(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var root = syntaxTree.GetRoot();

            // Find all invocation expressions
            var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                // Check if the invocation is a method call on a member access expression
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    var methodName = memberAccess.Name.Identifier.Text;

                    if (forbiddenMethods.Contains(methodName))
                    {
                        // To be very specific, we look for calls like `Database.Migrate()` or `Database.OpenConnection()`
                        if (memberAccess.Expression.ToString().EndsWith("Database", StringComparison.Ordinal))
                        {
                            offendingFiles.Add($"{file}: {methodName}() is called synchronously.");
                        }
                    }
                }
            }
        }

        offendingFiles.Should().BeEmpty("Because synchronous database methods (Migrate, OpenConnection, CanConnect) are not supported with tenant-scoped connection resolution interceptors. Use the *Async equivalents.");
    }
}
