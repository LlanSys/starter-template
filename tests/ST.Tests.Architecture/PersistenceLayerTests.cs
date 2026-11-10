using ST.Domain.Shared.Contracts.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;
using System.Reflection;

namespace ST.Tests.Architecture;

/// <summary>
/// Enforces structural rules within the Persistence layer.
/// </summary>
public sealed class PersistenceLayerTests
{
    // ── Entity Configurations ─────────────────────────────────────────────────

    [Fact]
    public void EntityConfigurations_Should_Be_Named_With_Configuration_Suffix()
    {
        var result = Types.InAssembly(AssemblyReferences.Persistence)
            .That()
            .ImplementInterface(typeof(IEntityTypeConfiguration<>))
            .And().AreNotAbstract()
            .And().AreNotGeneric()
            .Should()
            .HaveNameEndingWith("Configuration")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "All IEntityTypeConfiguration<T> implementations must end in 'Configuration'. " +
                     "Failing types: {0}", string.Join(", ", result.FailingTypes?.Select(t => t.Name) ?? []));
    }

    [Fact]
    public void EntityConfigurations_Should_Be_Internal()
    {
        var result = Types.InAssembly(AssemblyReferences.Persistence)
            .That()
            .ImplementInterface(typeof(IEntityTypeConfiguration<>))
            .Should()
            .NotBePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Entity configurations are internal persistence details " +
                     "and must not be public. Failing types: {0}", string.Join(", ",
                result.FailingTypes?.Select(t => t.Name) ?? []));
    }

    [Fact]
    public void EntityConfigurations_Should_Reside_In_Feature_Configuration_Namespaces()
    {
        var configurationTypes = Types.InAssembly(AssemblyReferences.Persistence)
            .That()
            .ImplementInterface(typeof(IEntityTypeConfiguration<>))
            .GetTypes();

        var misplacedTypes = configurationTypes
            .Where(t => t.Namespace is null ||
                        !t.Namespace.StartsWith("ST.Persistence.Features.", StringComparison.Ordinal) ||
                        !t.Namespace.Contains(".EntityConfigurations", StringComparison.Ordinal))
            .Select(t => t.FullName)
            .ToList();

        misplacedTypes.Should().BeEmpty(
            because: "entity configurations must live under their owning Persistence feature folder. Found: {0}",
            string.Join(", ", misplacedTypes));
    }

    [Fact]
    public void Declared_DbSet_Entities_Should_Have_Explicit_Configurations()
    {
        var configuredEntityTypes = AssemblyReferences.Persistence
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
            .SelectMany(GetConfiguredEntityTypes)
            .ToHashSet();

        var dbSetEntityTypes = GetDeclaredDbSetEntityTypes();

        var missingConfigurations = dbSetEntityTypes
            .Where(t => !configuredEntityTypes.Contains(t))
            .Select(t => t.FullName)
            .ToList();

        missingConfigurations.Should().BeEmpty(
            because: "DbSet entities should have explicit IEntityTypeConfiguration<T> mappings. Missing: {0}",
            string.Join(", ", missingConfigurations));
    }

    [Fact]
    public void Declared_DbSet_Entities_Should_Support_Soft_Delete()
    {
        var hardDeleteOnlyEntities = GetDeclaredDbSetEntityTypes()
            .Where(t => !typeof(ISoftDeletable).IsAssignableFrom(t) && t.Name is not ("PasskeyChallenge" or "SsoAuthorizationCode") &&
                        !(t.Namespace?.Contains(".ControlPlane.") ?? false) &&
                        !(t.Namespace?.Contains(".Shared.") ?? false))
            .Select(t => t.FullName)
            .ToList();

        hardDeleteOnlyEntities.Should().BeEmpty(
            because: "persisted bounded-context entities should support soft delete by default (excluding ControlPlane and Shared features). Missing: {0}",
            string.Join(", ", hardDeleteOnlyEntities));
    }

    [Fact]
    public void Declared_DbSet_Entities_Should_Be_Tenant_Scoped()
    {
        var entitiesWithoutTenantId = GetDeclaredDbSetEntityTypes()
            .Where(t => t.GetProperty("TenantId") is null &&
                        !(t.Namespace?.Contains(".ControlPlane.") ?? false))
            .Select(t => t.FullName)
            .ToList();

        entitiesWithoutTenantId.Should().BeEmpty(
            because: "persisted bounded-context entities must expose TenantId so DBContext query filters can enforce tenant isolation. Missing: {0}",
            string.Join(", ", entitiesWithoutTenantId));
    }

    [Fact]
    public void IamTokenRepository_Should_Not_Query_With_Unmapped_RefreshToken_Status_Helpers()
    {
        var repositoryPath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Backend",
            "Persistence",
            "ST.Persistence",
            "Features",
            "IAM",
            "Users",
            "Repositories",
            "IamTokenRepository.cs");

        var source = File.ReadAllText(repositoryPath);
        var forbiddenMembers = new[] { "IsActive", "IsExpired", "IsRevoked", "IsUsed" };
        var unsafeMembers = forbiddenMembers
            .Where(member => source.Contains($".{member}", StringComparison.Ordinal))
            .ToList();

        unsafeMembers.Should().BeEmpty(
            because: "RefreshToken status helpers are [NotMapped] convenience properties. " +
                     "Repository predicates must use mapped columns such as RevokedAt, ExpiresAt, and UsedAt. Found: {0}",
            string.Join(", ", unsafeMembers));
    }

    private static List<Type> GetDeclaredDbSetEntityTypes() =>
        Types.InAssembly(AssemblyReferences.Persistence)
            .That()
            .Inherit(typeof(DbContext))
            .GetTypes()
            .SelectMany(t => t.GetProperties(BindingFlags.Instance |
                                             BindingFlags.Public |
                                             BindingFlags.DeclaredOnly))
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .Select(p => p.PropertyType.GetGenericArguments()[0])
            .Distinct()
            .ToList();

    private static IEnumerable<Type> GetConfiguredEntityTypes(Type configurationType)
    {
        for (var current = configurationType; current is not null && current != typeof(object); current = current.BaseType)
        {
            if (current.IsGenericType &&
                current.GetGenericTypeDefinition().Name.StartsWith("BaseLookupConfiguration", StringComparison.Ordinal))
            {
                yield return current.GetGenericArguments()[0];
            }

            foreach (var interfaceType in current.GetInterfaces())
            {
                if (interfaceType.IsGenericType &&
                    interfaceType.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>))
                {
                    var configuredType = interfaceType.GetGenericArguments()[0];
                    if (!configuredType.IsGenericParameter)
                    {
                        yield return configuredType;
                    }
                }
            }
        }
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src")) &&
                Directory.Exists(Path.Combine(current.FullName, "tests")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from the test output directory.");
    }

    // ── DBContexts ────────────────────────────────────────────────────────────

    [Fact]
    public void DBContexts_Should_Reside_In_Known_DataContext_Namespaces()
    {
        var dbContextTypes = Types.InAssembly(AssemblyReferences.Persistence)
            .That()
            .Inherit(typeof(DbContext))
            .GetTypes()
            .ToList();

        dbContextTypes.Should().NotBeEmpty("Persistence must define EF Core DBContext types.");

        var allowedNamespaces = new[]
        {
            "ST.Persistence.Features.IAM.DataContext",
            "ST.Persistence.Features.Shared.DataContext",
            "ST.Persistence.Features.ControlPlane.DataContext"
        };

        var misplacedTypes = dbContextTypes
            .Where(t => t.Namespace is null || !allowedNamespaces.Contains(t.Namespace))
            .Select(t => t.FullName)
            .ToList();

        misplacedTypes.Should().BeEmpty(
            because: "DBContext subclasses must live in one of the bounded-context DataContext namespaces. Found: {0}",
            string.Join(", ", misplacedTypes));
    }

    [Fact]
    public void Required_Bounded_Context_DBContexts_Should_Exist()
    {
        var dbContextTypeNames = Types.InAssembly(AssemblyReferences.Persistence)
            .That()
            .Inherit(typeof(DbContext))
            .GetTypes()
            .Select(t => t.Name)
            .ToList();

        dbContextTypeNames.Should().Contain([
            "SharedDBContext",
            "IamDBContext",
        "ControlPlaneDBContext",
        ], because: "the modular monolith requires one DBContext per bounded context.");
    }

    [Fact]
    public void DBContexts_Should_Expose_CurrentTenantId_For_Global_Filters()
    {
        var dbContextsWithoutTenantContext = Types.InAssembly(AssemblyReferences.Persistence)
            .That()
            .Inherit(typeof(DbContext))
            .GetTypes()
            .Where(t => t.GetProperty("CurrentTenantId") is null &&
                        !(t.Namespace?.Contains(".ControlPlane.") ?? false))
            .Select(t => t.FullName)
            .ToList();

        dbContextsWithoutTenantContext.Should().BeEmpty(
            because: "DBContexts must expose CurrentTenantId so EF global query filters are tenant-parameterized per request (excluding ControlPlane). Missing: {0}",
            string.Join(", ", dbContextsWithoutTenantContext));
    }
}
