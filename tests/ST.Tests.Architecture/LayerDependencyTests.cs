using FluentAssertions;
using NetArchTest.Rules;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Tests.Architecture;

/// <summary>
/// Enforces Clean Architecture layer dependency rules.
/// </summary>
/// <remarks>
/// The golden rule: dependencies point inward only.
///
///   Shared ←──────────────────────────────┐
///   Domain ←── Application ←── Persistence │ (UI references all, but not tested here)
///
/// A violation here means someone has imported an outer-layer namespace
/// into an inner layer — a architectural regression that must not reach main.
/// These tests run in CI before any other checks.
/// </remarks>
public sealed class LayerDependencyTests
{
    private const string DomainNamespace = "ST.Domain";
    private const string ApplicationNamespace = "ST.Application";
    private const string PersistenceNamespace = "ST.Persistence";
    private const string SharedNamespace = "ST.Shared";

    // ── Domain ───────────────────────────────────────────────────────────────

    [Fact]
    public void Domain_ShouldNot_DependOn_Application()
    {
        var result = Types.InAssembly(AssemblyReferences.Domain)
            .ShouldNot()
            .HaveDependencyOn(ApplicationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain is the innermost layer and must have zero outward dependencies. " +
                     "Failing types: {0}", FormatFailingTypes(result));
    }

    [Fact]
    public void Domain_ShouldNot_DependOn_Persistence()
    {
        var result = Types.InAssembly(AssemblyReferences.Domain)
            .ShouldNot()
            .HaveDependencyOn(PersistenceNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain must not know about Persistence. " +
                     "Failing types: {0}", FormatFailingTypes(result));
    }

    // ── Application ──────────────────────────────────────────────────────────

    [Fact]
    public void Application_ShouldNot_DependOn_Persistence()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .ShouldNot()
            .HaveDependencyOn(PersistenceNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application must not reference Persistence directly — " +
                     "it communicates via IUnitOfWork / IRepository abstractions. " +
                     "Failing types: {0}", FormatFailingTypes(result));
    }

    [Fact]
    public void Application_Can_DependOn_Domain()
    {
        // Positive test — confirms the architecture is wired correctly,
        // not just that violations are absent.
        var result = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveDependencyOn(DomainNamespace)
            .GetTypes();

        result.Should().NotBeEmpty(
            because: "Application layer must reference Domain types (entities, interfaces). " +
                     "An empty result suggests the assembly anchor or namespace is wrong.");
    }

    // ── Persistence ──────────────────────────────────────────────────────────

    [Fact]
    public void Persistence_Can_DependOn_Domain()
    {
        // Persistence is allowed to depend on both — it implements Application
        // interfaces (IRepository, IUnitOfWork) against Domain entities.
        var appDependents = Types.InAssembly(AssemblyReferences.Persistence)
            .That()
            .HaveDependencyOn(DomainNamespace)
            .GetTypes();

        appDependents.Should().NotBeEmpty(
            because: "Persistence must implement Application contracts (IUnitOfWork, IRepository). " +
                     "An empty result suggests the assembly anchor or namespace is wrong.");
    }

    // ── Shared ────────────────────────────────────────────────────────────────

    [Fact]
    public void Shared_ShouldNot_DependOn_Domain()
    {
        var result = Types.InAssembly(AssemblyReferences.SharedKernel)
            .ShouldNot()
            .HaveDependencyOn(DomainNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Shared (DTOs, contracts) must not reference Domain entities — " +
                     "it is consumed by UI projects that do not reference Domain directly. " +
                     "Failing types: {0}", FormatFailingTypes(result));
    }

    [Fact]
    public void Shared_ShouldNot_DependOn_Persistence()
    {
        var result = Types.InAssembly(AssemblyReferences.SharedKernel)
            .ShouldNot()
            .HaveDependencyOn(PersistenceNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Shared must never reference infrastructure concerns. " +
                     "Failing types: {0}", FormatFailingTypes(result));
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private static string FormatFailingTypes(TestResult result)
        => result.FailingTypes is { Count: > 0 }
            ? string.Join(", ", result.FailingTypes.Select(t => t.FullName))
            : "none";
}
