using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using NetArchTest.Rules;
using ST.Domain.Features.ControlPlane.Tenants.Contracts.Repositories;
using ST.Tests.Architecture.Features.BoundaryProbe;

namespace ST.Tests.Architecture;

public sealed class ContextDependencyTests
{
    private const string ApplicationFeatures = "ST.Application.Features.";
    private const string InfrastructureFeatures = "ST.Infrastructure.Features.";
    private const string DomainFeatures = "ST.Domain.Features.";
    private const string PersistenceFeatures = "ST.Persistence.Features.";

    private static readonly string[] PlatformContexts = ["IAM", "ControlPlane", "Shared"];

    private static readonly Assembly[] FeatureAssemblies =
    [
        AssemblyReferences.Domain, AssemblyReferences.Application, AssemblyReferences.Infrastructure,
        AssemblyReferences.Persistence, AssemblyReferences.Api, AssemblyReferences.SharedKernel,
        AssemblyReferences.SharedKernelValidation
    ];

    private static readonly Dictionary<string, string[]> ForeignUnitOfWorkCallers = new(StringComparer.Ordinal)
    {
        ["IAM"] =
        [
            ApplicationFeatures + "Shared.Dashboard.QueryHandlers.GetDashboardSummaryQueryHandler",
            InfrastructureFeatures + "ControlPlane.Demo.EnterDemoHandler",
            InfrastructureFeatures + "ControlPlane.Demo.ResetDemoHandler",
            InfrastructureFeatures + "ControlPlane.Modules.ApplyRoleTemplateHandler",
            InfrastructureFeatures + "ControlPlane.Modules.EffectiveModulePolicy",
            InfrastructureFeatures + "ControlPlane.Provisioning.TenantWorkspaceProvisioner",
            InfrastructureFeatures + "ControlPlane.Staff.PlatformStaffService",
            InfrastructureFeatures + "ControlPlane.Support.SupportHandler",
            InfrastructureFeatures + "ControlPlane.Tenants.Services.AdministratorInvitationService"
        ],
        ["ControlPlane"] =
        [
            InfrastructureFeatures + "IAM.AspNetCoreIdentity.CommandHandlers.AcceptAccountInvitation",
            InfrastructureFeatures + "IAM.AspNetCoreIdentity.CommandHandlers.ExchangeSsoCode",
            InfrastructureFeatures + "IAM.AspNetCoreIdentity.CommandHandlers.GetCurrentUser",
            InfrastructureFeatures + "IAM.AspNetCoreIdentity.CommandHandlers.InviteAdminUser",
            InfrastructureFeatures + "IAM.AspNetCoreIdentity.CommandHandlers.RefreshToken",
            InfrastructureFeatures + "IAM.Users.Contracts.Implementations.Services.SessionService",
            InfrastructureFeatures + "Shared.Dashboard.GetDashboardAttentionQueryHandler",
            InfrastructureFeatures + "Shared.WorkTasks.WorkTaskService",
            InfrastructureFeatures + "Shared.WorkTasks.WorkTaskSourceQuery"
        ]
    };

    private static readonly Dictionary<string, string[]> ForeignPersistenceCallers = new(StringComparer.Ordinal)
    {
        ["IAM"] =
        [
            InfrastructureFeatures + "ControlPlane.Demo.EnterDemoHandler",
            InfrastructureFeatures + "ControlPlane.Demo.NavigationDemoFixture",
            InfrastructureFeatures + "ControlPlane.Demo.ResetDemoHandler",
            InfrastructureFeatures + "ControlPlane.Provisioning.TenantDatabaseReadiness",
            InfrastructureFeatures + "ControlPlane.Staff.PlatformStaffService",
            InfrastructureFeatures + "Shared.WorkTasks.TaskAssigneeEligibility",
            InfrastructureFeatures + "Shared.WorkTasks.WorkTaskService"
        ],
        ["ControlPlane"] = [InfrastructureFeatures + "IAM.Authorization.TenantAccountLimitProvider"],
        ["Shared"] = [InfrastructureFeatures + "ControlPlane.Provisioning.TenantDatabaseReadiness"]
    };

    [Fact]
    public void Platform_And_Product_Features_Respect_Iam_Ownership()
    {
        var productContexts = FeatureAssemblies
            .SelectMany(GetFeatureContexts)
            .Except(PlatformContexts, StringComparer.Ordinal)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var productFeaturePrefixes = FeatureAssemblies
            .SelectMany(assembly => productContexts.Select(context => FeatureNamespace(assembly, context)))
            .ToArray();

        var iamEntityPrefixes = AssemblyReferences.Domain.GetTypes()
            .Select(type => type.Namespace)
            .OfType<string>()
            .Where(ns => ns.StartsWith(DomainFeatures + "IAM.", StringComparison.Ordinal) &&
                         ns.EndsWith(".Entities", StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .Select(ns => ns + ".");

        var iamInternals = iamEntityPrefixes
            .Append(InfrastructureFeatures + "IAM.")
            .Append(PersistenceFeatures + "IAM.")
            .ToArray();

        var violations = new List<string>();

        foreach (var assembly in FeatureAssemblies)
        {
            violations.AddRange(FindViolations(assembly, "IAM", productFeaturePrefixes, []));

            foreach (var context in productContexts)
            {
                violations.AddRange(FindViolations(assembly, context, iamInternals, []));
            }
        }

        violations.Should().BeEmpty(
            because: "product contexts use IAM through stable contracts and IAM is independent of product features");
    }

    [Fact]
    public void Feature_Workflows_Use_Their_Own_Context_Persistence()
    {
        var violations = new List<string>();
        var domainTypes = AssemblyReferences.Domain.GetTypes().Where(type => type.IsInterface).ToArray();
        var domainContexts = GetFeatureContexts(AssemblyReferences.Domain).ToArray();

        foreach (var assembly in new[] { AssemblyReferences.Application, AssemblyReferences.Infrastructure })
        {
            foreach (var context in GetFeatureContexts(assembly))
            {
                foreach (var owner in domainContexts.Where(owner => owner != context))
                {
                    var dataPorts = domainTypes
                        .Where(type => type.Namespace?.StartsWith(DomainFeatures + owner + ".", StringComparison.Ordinal) == true)
                        .Where(type => type.Name.EndsWith("UnitOfWork", StringComparison.Ordinal) ||
                                       type.Namespace!.EndsWith(".Contracts.Repositories", StringComparison.Ordinal))
                        .Select(type => type.FullName!)
                        .ToArray();

                    var unitCallers = ForeignUnitOfWorkCallers.GetValueOrDefault(owner) ?? [];
                    var persistenceCallers = ForeignPersistenceCallers.GetValueOrDefault(owner) ?? [];

                    violations.AddRange(FindViolations(assembly, context, dataPorts, unitCallers));
                    violations.AddRange(FindViolations(
                        assembly, context, [PersistenceFeatures + owner + "."], persistenceCallers));
                }
            }
        }

        violations.Should().BeEmpty(
            because: "cross-context data access belongs behind the owning context or in a named platform workflow");
    }

    [Fact]
    public void Rule_Detects_A_Foreign_Repository_Resolved_Inside_A_Method()
    {
        var fixture = typeof(ForeignRepositoryProbe);
        string[] dependencies = [typeof(ITenantRepository).FullName!];

        FindViolations(fixture.Assembly, "BoundaryProbe", dependencies, []).Should().ContainSingle()
            .Which.Should().Contain(fixture.FullName);

        FindViolations(fixture.Assembly, "BoundaryProbe", dependencies, [fixture.FullName!])
            .Should().BeEmpty();
    }

    private static IEnumerable<string> GetFeatureContexts(Assembly assembly)
    {
        var prefix = FeatureNamespace(assembly, string.Empty);

        return assembly.GetTypes()
            .Select(type => type.Namespace)
            .OfType<string>()
            .Where(ns => ns.StartsWith(prefix, StringComparison.Ordinal))
            .Select(ns => ns[prefix.Length..].Split('.')[0])
            .Where(context => context.Length > 0)
            .Distinct(StringComparer.Ordinal);
    }

    private static string FeatureNamespace(Assembly assembly, string context)
    {
        var prefix = assembly.GetName().Name + ".Features.";

        return context.Length == 0 ? prefix : prefix + context + ".";
    }

    private static IEnumerable<string> FindViolations(
        Assembly assembly, string context, string[] dependencies, string[] allowedCallers)
    {
        if (dependencies.Length == 0)
        {
            return [];
        }

        var contextNamespace = FeatureNamespace(assembly, context).TrimEnd('.');
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespaceMatching("^" + Regex.Escape(contextNamespace) + @"(?:\.|$)")
            .ShouldNot()
            .HaveDependencyOnAny(dependencies)
            .GetResult();

        return (result.FailingTypeNames ?? [])
            .Where(name => !allowedCallers.Contains(name.Split('+', '/')[0], StringComparer.Ordinal))
            .Select(name => $"{name} has a dependency on one of [{string.Join(", ", dependencies)}]");
    }
}
