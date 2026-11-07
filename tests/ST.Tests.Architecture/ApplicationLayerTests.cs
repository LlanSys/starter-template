using FluentAssertions;
using MediatR;
using NetArchTest.Rules;
using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace ST.Tests.Architecture;

/// <summary>
/// Enforces structural rules within the Application layer — CQRS conventions,
/// handler registration, and feature slice structure.
/// </summary>
public sealed class ApplicationLayerTests
{
    // ── Commands ──────────────────────────────────────────────────────────────

    [Fact]
    public void Commands_Should_Be_Named_With_Command_Suffix()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .ImplementInterface(typeof(IRequest<>))
            .And()
            .HaveNameEndingWith("Command")
            .Should()
            .HaveNameEndingWith("Command")
            .GetResult();

        // Inverse — find IRequest<T> implementors that aren't Query or Command
        var unnamedRequests = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .ImplementInterface(typeof(IRequest<>))
            .And()
            .DoNotHaveNameEndingWith("Command")
            .And()
            .DoNotHaveNameEndingWith("Query")
            .GetTypes();

        unnamedRequests.Should().BeEmpty(
            because: "Every IRequest<T> in Application must be named *Command or *Query " +
                     "to make CQRS intent explicit. Unnamed types: {0}",
            string.Join(", ", unnamedRequests.Select(t => t.Name)));
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    [Fact]
    public void Queries_Should_Be_Named_With_Query_Suffix()
    {
        var unnamedQueries = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .ImplementInterface(typeof(IRequest<>))
            .And()
            .DoNotHaveNameEndingWith("Query")
            .And()
            .DoNotHaveNameEndingWith("Command")
            .GetTypes();

        unnamedQueries.Should().BeEmpty(
            because: "Every IRequest<T> must end in Query or Command. " +
                     "Violators: {0}",
            string.Join(", ", unnamedQueries.Select(t => t.Name)));
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    [Fact]
    public void Handlers_Should_Be_Named_With_Handler_Suffix()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .ImplementInterface(typeof(IRequestHandler<,>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "All IRequestHandler<,> implementations must end in 'Handler'. " +
                     "Failing types: {0}", string.Join(", ",
                result.FailingTypes?.Select(t => t.Name) ?? []));
    }

    [Fact]
    public void Handlers_Should_Be_Internal()
    {
        // Handlers are implementation details — they must never be referenced
        // directly by outer layers. MediatR discovers them via DI registration.
        var result = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .ImplementInterface(typeof(IRequestHandler<,>))
            .Should()
            .NotBePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Handlers are internal implementation details. " +
                     "They must be internal or private, never public. " +
                     "Failing types: {0}", string.Join(", ",
                result.FailingTypes?.Select(t => t.Name) ?? []));
    }

    // ── Validators ────────────────────────────────────────────────────────────

    [Fact]
    public void Validators_Should_Be_Named_With_Validator_Suffix()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .Inherit(typeof(AbstractValidator<>))
            .Should()
            .HaveNameEndingWith("Validator")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "All AbstractValidator<T> subclasses must end in 'Validator'. " +
                     "Failing types: {0}", string.Join(", ",
                result.FailingTypes?.Select(t => t.Name) ?? []));
    }

    // ── Feature slice structure ───────────────────────────────────────────────

    [Fact]
    public void Handlers_Should_Reside_In_Features_Namespace()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .ImplementInterface(typeof(IRequestHandler<,>))
            .Should()
            .ResideInNamespaceStartingWith("ST.Application.Features")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "All handlers must live inside ST.Application.Features.* " +
                     "to enforce VSA feature-slice structure. " +
                     "Failing types: {0}", string.Join(", ",
                result.FailingTypes?.Select(t => t.Name) ?? []));
    }

    [Fact]
    public void Queries_Should_Declare_Cache_Strategy()
    {
        var queriesWithoutCacheStrategy = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .ImplementInterface(typeof(IRequest<>))
            .And()
            .HaveNameEndingWith("Query")
            .GetTypes()
            .Where(t => !ImplementsInterface(t, "ST.Application.Contracts.Interfaces.Common.ICachableRequest"))
            .Select(t => t.FullName)
            .ToList();

        queriesWithoutCacheStrategy.Should().BeEmpty(
            because: "read queries should explicitly opt into the cache pipeline and declare their cache key strategy. Found: {0}",
            string.Join(", ", queriesWithoutCacheStrategy));
    }

    [Fact]
    public void Application_Should_Not_Reference_Entity_Framework_Core()
    {
        var forbiddenReferences = AssemblyReferences.Application
            .GetReferencedAssemblies()
            .Where(reference => reference.Name is not null &&
                                reference.Name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal))
            .Select(reference => reference.Name)
            .ToList();

        forbiddenReferences.Should().BeEmpty(
            because: "Application may compose LINQ intent through repository abstractions, but EF Core execution details belong in Persistence. Found: {0}",
            string.Join(", ", forbiddenReferences));
    }

    [Fact]
    public void Permission_And_Menu_Write_Commands_Should_Declare_Cache_Invalidation()
    {
        var writeCommandsWithoutInvalidation = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .ImplementInterface(typeof(IRequest<>))
            .And()
            .HaveNameEndingWith("Command")
            .GetTypes()
            .Where(t => t.Namespace is not null &&
                        (t.Namespace.StartsWith("ST.Application.Features.IAM.Menus.Commands", StringComparison.Ordinal) ||
                         t.Namespace.StartsWith("ST.Application.Features.IAM.Permissions.Commands", StringComparison.Ordinal)))
            .Where(t => !ImplementsInterface(t, "ST.Application.Contracts.Interfaces.Common.ICacheInvalidatorRequest"))
            .Select(t => t.FullName)
            .ToList();

        writeCommandsWithoutInvalidation.Should().BeEmpty(
            because: "Permission and menu write commands mutate data used by cached reads and must declare invalidation keys. Found: {0}",
            string.Join(", ", writeCommandsWithoutInvalidation));
    }

    [Fact]
    public void Iam_Application_Artifacts_Should_Reside_In_Approved_Features()
    {
        var applicationRoot = Path.Combine(
            AssemblyReferences.RepoRoot,
            "src",
            "Backend",
            "Application",
            "ST.Application");

        var iamRoot = Path.Combine(applicationRoot, "Features", "IAM");
        string[] approvedFeatureRoots = ["Users", "Permissions", "Menus", "ReferenceData"];

        var misplacedArtifacts = Directory
            .EnumerateFiles(iamRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
                !approvedFeatureRoots.Any(feature => path.Contains($"{Path.DirectorySeparatorChar}{feature}{Path.DirectorySeparatorChar}", StringComparison.Ordinal)) &&
                (path.Contains($"{Path.DirectorySeparatorChar}Commands{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                 path.Contains($"{Path.DirectorySeparatorChar}EventHandlers{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                 path.Contains($"{Path.DirectorySeparatorChar}Mappings{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
                 path.Contains($"{Path.DirectorySeparatorChar}IntegrationEvents{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
            .Select(path => Path.GetRelativePath(applicationRoot, path))
            .ToList();

        misplacedArtifacts.Should().BeEmpty(
            because: "IAM application artifacts belong under approved IAM feature slices. Found: {0}",
            string.Join(", ", misplacedArtifacts));
    }

    private static bool ImplementsInterface(Type type, string interfaceFullName)
    {
        return type.GetInterfaces()
            .Any(i => string.Equals(i.FullName, interfaceFullName, StringComparison.Ordinal));
    }
}
