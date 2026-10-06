using FluentAssertions;
using FluentValidation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NetArchTest.Rules;
using System;
using System.Collections.Generic;

namespace ST.Tests.Architecture;

/// <summary>
/// Enforces naming conventions that the compiler and editorconfig cannot catch —
/// specifically, conventions on types across assembly and namespace boundaries.
/// </summary>
public sealed class NamingConventionTests
{
    // ── Interfaces ────────────────────────────────────────────────────────────

    [Fact]
    public void All_Interfaces_Should_Start_With_I()
    {
        var assemblies = new[]
        {
            AssemblyReferences.Domain,
            AssemblyReferences.Application,
            AssemblyReferences.Persistence,
        };

        foreach (var assembly in assemblies)
        {
            var result = Types.InAssembly(assembly)
                .That()
                .AreInterfaces()
                .Should()
                .HaveNameStartingWith("I")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                because: $"All interfaces in {assembly.GetName().Name} must start with 'I'. " +
                         "Failing types: {0}", string.Join(", ",
                    result.FailingTypes?.Select(t => t.FullName) ?? []));
        }
    }

    // ── Async methods — checked via Roslyn/editorconfig, not NetArchTest ──────
    // NetArchTest reflects on types, not method signatures. Async method naming
    // (must end in Async) is enforced by the dotnet_naming_rule in .editorconfig.

    // ── Exception classes ─────────────────────────────────────────────────────

    [Fact]
    public void Exceptions_Should_End_With_Exception()
    {
        var assemblies = new[]
        {
            AssemblyReferences.Domain,
            AssemblyReferences.Application,
        };

        foreach (var assembly in assemblies)
        {
            var result = Types.InAssembly(assembly)
                .That()
                .Inherit(typeof(Exception))
                .Should()
                .HaveNameEndingWith("Exception")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                because: $"All Exception subclasses in {assembly.GetName().Name} must end in 'Exception'. " +
                         "Failing types: {0}", string.Join(", ",
                    result.FailingTypes?.Select(t => t.Name) ?? []));
        }
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Dtos_Should_Reside_In_Shared_Project()
    {
        // Nothing in Domain or Persistence should be named *Dto —
        // DTOs belong exclusively in ST.Shared.
        var domainDtos = Types.InAssembly(AssemblyReferences.Domain)
            .That()
            .HaveNameEndingWith("Dto")
            .GetTypes();

        var persistenceDtos = Types.InAssembly(AssemblyReferences.Persistence)
            .That()
            .HaveNameEndingWith("Dto")
            .GetTypes();

        domainDtos.Should().BeEmpty(
            because: "DTOs must live in ST.Shared, not ST.Domain. " +
                     "Found in Domain: {0}", string.Join(", ", domainDtos.Select(t => t.Name)));

        persistenceDtos.Should().BeEmpty(
            because: "DTOs must live in ST.Shared, not ST.Persistence. " +
                     "Found in Persistence: {0}", string.Join(", ", persistenceDtos.Select(t => t.Name)));
    }

    [Fact]
    public void SharedKernel_FeatureOwned_Types_Should_Reside_In_Features_Namespace()
    {
        var misplacedTypes = Types.InAssembly(AssemblyReferences.SharedKernel)
            .That()
            .ResideInNamespaceStartingWith("ST.SharedKernel.Dtos.Auth")
            .Or()
            .ResideInNamespaceStartingWith("ST.SharedKernel.Dtos.Employees")
            .Or()
            .ResideInNamespaceStartingWith("ST.SharedKernel.Dtos.Directors")
            .Or()
            .ResideInNamespaceStartingWith("ST.SharedKernel.Dtos.Dashboard")
            .Or()
            .ResideInNamespaceStartingWith("ST.SharedKernel.Dtos.Lookups")
            .Or()
            .ResideInNamespaceStartingWith("ST.SharedKernel.Dtos.Banking")
            .Or()
            .ResideInNamespaceStartingWith("ST.SharedKernel.Enums")
            .GetTypes();

        misplacedTypes.Should().BeEmpty(
            because: "feature-owned SharedKernel DTOs and enums must live under ST.SharedKernel.Features.*. Found: {0}",
            string.Join(", ", misplacedTypes.Select(t => t.FullName)));
    }

    [Fact]
    public void SharedKernel_Validators_Should_Reside_In_Features_Or_Common_Namespace()
    {
        var validatorTypes = Types.InAssembly(AssemblyReferences.SharedKernelValidation)
            .That()
            .Inherit(typeof(AbstractValidator<>))
            .GetTypes();

        var misplacedTypes = validatorTypes
            .Where(t => t.Namespace is null ||
                        (!t.Namespace.StartsWith("ST.SharedKernel.Validation.Features.", StringComparison.Ordinal) &&
                         !t.Namespace.StartsWith("ST.SharedKernel.Validation.Validators.Common", StringComparison.Ordinal)))
            .Select(t => t.FullName)
            .ToList();

        misplacedTypes.Should().BeEmpty(
            because: "feature-owned validators must live under ST.SharedKernel.Validation.Features.*; only generic validator bases stay in Common. Found: {0}",
            string.Join(", ", misplacedTypes));
    }

    [Fact]
    public void Api_Controllers_Should_Reside_In_Features_Namespace()
    {
        var apiRoot = Path.Combine(
            AssemblyReferences.RepoRoot,
            "src",
            "Backend",
            "Api",
            "ST.Api");

        var misplacedControllers = Directory
            .EnumerateFiles(apiRoot, "*Controller.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Common{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                           !path.Contains($"{Path.DirectorySeparatorChar}Features{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(apiRoot, path))
            .ToList();

        misplacedControllers.Should().BeEmpty(
            because: "feature-owned API controllers must live under ST.Api.Features by bounded context and feature. Found: {0}",
            string.Join(", ", misplacedControllers));
    }

    [Fact]
    public void Public_Top_Level_Types_Should_Have_One_Type_Per_File()
    {
        var authoredSourceRoots = new[]
        {
            Path.Combine(AssemblyReferences.RepoRoot, "src"),
            Path.Combine(AssemblyReferences.RepoRoot, "tests"),
        };

        var violations = authoredSourceRoots
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(IsAuthoredSourceFile)
            .Select(path => new
            {
                Path = path,
                Types = GetPublicTopLevelTypes(path),
            })
            .Where(file => file.Types.Count > 1 ||
                           file.Types.Any(type => !IsExpectedTypeFileName(file.Path, type)))
            .Select(file =>
            {
                var relativePath = Path.GetRelativePath(AssemblyReferences.RepoRoot, file.Path);
                var types = string.Join(", ", file.Types.Select(type => $"{type.Name} at line {type.Line}"));
                return $"{relativePath}: {types}";
            })
            .ToList();

        violations.Should().BeEmpty(
            because: "each public top-level class, record, struct, interface, and enum must live in its own file named after that type. Found: {0}",
            string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Custom_Context_Related_Types_Should_Use_DB_Acronym()
    {
        var assemblies = new[]
        {
            AssemblyReferences.Api,
            AssemblyReferences.Application,
            AssemblyReferences.Persistence,
            AssemblyReferences.SharedKernel,
            AssemblyReferences.SharedKernelValidation,
        };

        var violations = assemblies
            .SelectMany(static assembly => assembly.GetTypes())
            .Where(static type => type.Namespace is not null &&
                                  type.Namespace.StartsWith("ST.", StringComparison.Ordinal))
            .Where(static type => type.Name.Contains("DbContext", StringComparison.Ordinal) ||
                                  type.Name.Contains("DbContextFactory", StringComparison.Ordinal) ||
                                  type.Name.Contains("DbContextHelper", StringComparison.Ordinal))
            .Select(static type => type.FullName)
            .Distinct()
            .ToList();

        violations.Should().BeEmpty(
            because: "custom context-related types should use the DB acronym, for example IamDBContext, IamDBContextFactory, and DBContextHelper. Found: {0}",
            string.Join(", ", violations));
    }

    private static List<PublicTopLevelType> GetPublicTopLevelTypes(string path)
    {
        var sourceText = File.ReadAllText(path);
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, path: path);
        var root = syntaxTree.GetCompilationUnitRoot();

        return root
            .DescendantNodes(static node => node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
            .Where(static node => node.Parent is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
            .Select(TryCreatePublicTopLevelType)
            .Where(static type => type is not null)
            .Select(static type => type!)
            .ToList();
    }

    private static PublicTopLevelType? TryCreatePublicTopLevelType(SyntaxNode node)
    {
        return node switch
        {
            BaseTypeDeclarationSyntax typeDeclaration when IsPublic(typeDeclaration.Modifiers) =>
                new PublicTopLevelType(
                    typeDeclaration.Identifier.ValueText,
                    GetLine(typeDeclaration),
                    IsPartial(typeDeclaration.Modifiers)),

            BaseNamespaceDeclarationSyntax => null,
            CompilationUnitSyntax => null,
            _ => null
        };
    }

    private static bool IsExpectedTypeFileName(string path, PublicTopLevelType type)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);

        return string.Equals(type.Name, fileName, StringComparison.Ordinal) ||
               (type.IsPartial &&
                fileName.StartsWith($"{type.Name}.", StringComparison.Ordinal));
    }

    private static bool IsPublic(SyntaxTokenList modifiers) =>
        modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PublicKeyword));

    private static bool IsPartial(SyntaxTokenList modifiers) =>
        modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.PartialKeyword));

    private static int GetLine(SyntaxNode node) =>
        node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;

    private static bool IsAuthoredSourceFile(string path)
    {
        var directorySeparator = Path.DirectorySeparatorChar.ToString();

        return !path.Contains($"{directorySeparator}bin{directorySeparator}", StringComparison.OrdinalIgnoreCase) &&
               !path.Contains($"{directorySeparator}obj{directorySeparator}", StringComparison.OrdinalIgnoreCase) &&
               !path.Contains($"{directorySeparator}Migrations{directorySeparator}", StringComparison.OrdinalIgnoreCase) &&
               !path.Contains($"{directorySeparator}Platforms{directorySeparator}", StringComparison.OrdinalIgnoreCase) &&
               !path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) &&
               !path.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase) &&
               !path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record PublicTopLevelType(string Name, int Line, bool IsPartial);
}
