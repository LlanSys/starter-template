using FluentAssertions;
using NetArchTest.Rules;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ST.Tests.Architecture;

/// <summary>
/// Enforces the solution-wide dependency graph.
/// </summary>
/// <remarks>
/// Approved dependency map:
/// <code>
/// Domain                  → nothing
/// SharedKernel            → nothing
/// SharedKernel.Validation → Domain, SharedKernel
/// Application             → Domain, SharedKernel
/// Infrastructure          → Application, SharedKernel
/// Persistence             → Domain, Application
/// API                     → Application, SharedKernel       (MAUI + Angular consumers)
/// RCL                     → SharedKernel                    (components only — no MediatR)
/// BlazorWebApp            → Application, SharedKernel, RCL  (direct — no HTTP overhead)
/// MAUI.BlazorHybrid       → SharedKernel, RCL               (HTTP → API)
/// </code>
///
/// BlazorWebApp calls the backend API over HTTP.
/// MAUI and future Angular clients go through the API over HTTP.
/// RCL components receive data as parameters — they never dispatch MediatR commands.
/// If RCL referenced Application, components would be coupled to MediatR and
/// unusable from MAUI which has no Application reference.
/// </remarks>
public sealed class ProjectDependencyTests
{
    // Namespace constants — single place to update if projects are renamed
    private const string NsDomain = "ST.Domain";
    private const string NsApplication = "ST.Application";
    private const string NsPersistence = "ST.Persistence";
    private const string NsInfrastructure = "ST.Infrastructure";
    private const string NsSharedKernel = "ST.SharedKernel";
    private const string NsApi = "ST.Api";

    private static readonly string[] IamForbiddenDependencies = ["ST.Application.Features.Banking", "ST.Application.Features.HR"];

    // ═════════════════════════════════════════════════════════════════════════
    // DOMAIN — depends on nothing
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Domain_ShouldNot_DependOn_Application()
        => AssertNoDependency(AssemblyReferences.Domain, NsApplication);

    [Fact]
    public void Domain_ShouldNot_DependOn_Persistence()
        => AssertNoDependency(AssemblyReferences.Domain, NsPersistence);

    [Fact]
    public void Domain_ShouldNot_DependOn_Infrastructure()
        => AssertNoDependency(AssemblyReferences.Domain, NsInfrastructure);

    [Fact]
    public void Domain_ShouldNot_DependOn_SharedKernel()
        => AssertNoDependency(AssemblyReferences.Domain, NsSharedKernel,
            because: "Domain defines its own contracts and must not depend on SharedKernel DTOs. " +
                     "SharedKernel.Validation may reference Domain, never the reverse.");

    // ═════════════════════════════════════════════════════════════════════════
    // SHAREDKERNEL — depends on nothing
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SharedKernel_ShouldNot_DependOn_Domain()
        => AssertNoDependency(AssemblyReferences.SharedKernel, NsDomain,
            because: "SharedKernel holds DTOs consumed by UI projects that have no Domain reference. " +
                     "A Domain dependency here would force UI projects to reference Domain transitively.");

    [Fact]
    public void SharedKernel_ShouldNot_DependOn_Application()
        => AssertNoDependency(AssemblyReferences.SharedKernel, NsApplication);

    [Fact]
    public void SharedKernel_ShouldNot_DependOn_Persistence()
        => AssertNoDependency(AssemblyReferences.SharedKernel, NsPersistence);

    [Fact]
    public void SharedKernel_ShouldNot_DependOn_Infrastructure()
        => AssertNoDependency(AssemblyReferences.SharedKernel, NsInfrastructure);

    // ═════════════════════════════════════════════════════════════════════════
    // APPLICATION — depends on Domain + SharedKernel only
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Application_ShouldNot_DependOn_Persistence()
        => AssertNoDependency(AssemblyReferences.Application, NsPersistence,
            because: "Application uses IRepository/IUnitOfWork abstractions. " +
                     "Persistence implements those contracts — Application must never " +
                     "reference the implementation directly.");

    [Fact]
    public void Application_ShouldNot_DependOn_Infrastructure()
        => AssertNoDependency(AssemblyReferences.Application, NsInfrastructure,
            because: "Application depends on Infrastructure interfaces (IEmailService etc.), " +
                     "not the concrete Infrastructure implementations.");

    [Fact]
    public void Application_ShouldNot_DependOn_Api()
        => AssertNoDependency(AssemblyReferences.Application, NsApi);

    [Fact]
    public void Application_Should_DependOn_Domain()
    {
        // Positive test — confirms the assembly anchor is correct and the
        // Application layer is actually wired to Domain as expected.
        var types = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveDependencyOn(NsDomain)
            .GetTypes();

        types.Should().NotBeEmpty(
            because: "Application must reference Domain types (entities, interfaces, value objects). " +
                     "An empty result means the assembly anchor type is wrong.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PERSISTENCE — depends on Domain + Application
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Persistence_ShouldNot_DependOn_Infrastructure()
        => AssertNoDependency(AssemblyReferences.Persistence, NsInfrastructure);

    [Fact]
    public void Persistence_ShouldNot_DependOn_Api()
        => AssertNoDependency(AssemblyReferences.Persistence, NsApi);

    [Fact]
    public void Persistence_Should_DependOn_Domain()
    {
        var types = Types.InAssembly(AssemblyReferences.Persistence)
            .That()
            .HaveDependencyOn(NsDomain)
            .GetTypes();

        types.Should().NotBeEmpty(
            because: "Persistence must reference Domain entities to map them via EF Core configurations.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // SHAREDKERNEL — positive confirmation tests
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SharedKernel_Should_HaveTypes()
    {
        // Sanity check — confirms the assembly anchor resolved correctly.
        var types = Types.InAssembly(AssemblyReferences.SharedKernel)
            .GetTypes();

        types.Should().NotBeEmpty(
            because: "SharedKernel assembly resolved to an empty assembly — " +
                     "check that the anchor type in AssemblyReferences.cs is correct.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // BOUNDED CONTEXT ISOLATION (Application Layer Features)
    // ═════════════════════════════════════════════════════════════════════════

    [Fact]
    public void StarterAssembliesShouldNotShipBankingOrHrFeatures()
    {
        var assemblies = new[] { AssemblyReferences.Domain, AssemblyReferences.Application,
            AssemblyReferences.Persistence, AssemblyReferences.Api, AssemblyReferences.SharedKernel };
        var businessTypes = assemblies.SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.Namespace?.Contains(".Features.Banking", StringComparison.Ordinal) == true
                || type.Namespace?.Contains(".Features.HR", StringComparison.Ordinal) == true);
        businessTypes.Should().BeEmpty("application business contexts belong in downstream repositories");
    }

    [Fact]
    public void IAM_Module_ShouldNot_DependOn_Banking_Or_HR()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .ResideInNamespace("ST.Application.Features.IAM")
            .ShouldNot()
            .HaveDependencyOnAny(IamForbiddenDependencies)
            .GetResult();

        var failingTypeNames = result.FailingTypes is { Count: > 0 }
            ? string.Join(", ", result.FailingTypes.Select(t => t.FullName))
            : string.Empty;

        result.IsSuccessful.Should().BeTrue(
            because: $"IAM feature module must not depend on Banking or HR modules directly. Failing types: {failingTypeNames}");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PRIVATE HELPER — reduces repetition across negative dependency assertions
    // ═════════════════════════════════════════════════════════════════════════

    private static void AssertNoDependency(
        Assembly assembly,
        string forbiddenNamespace,
        string? because = null)
    {
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOn(forbiddenNamespace)
            .GetResult();

        var failingTypeNames = result.FailingTypes is { Count: > 0 }
            ? string.Join(", ", result.FailingTypes.Select(t => t.FullName))
            : string.Empty;

        result.IsSuccessful.Should().BeTrue(
            because: because is not null
                ? $"{because} Failing types: {failingTypeNames}"
                : $"{assembly.GetName().Name} must not depend on {forbiddenNamespace}. " +
                  $"Failing types: {failingTypeNames}");
    }
}
