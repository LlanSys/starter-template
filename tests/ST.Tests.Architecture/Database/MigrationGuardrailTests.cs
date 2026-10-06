using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.ControlPlane.DataContext;
using ST.Persistence.Features.IAM.DataContext;
using ST.Persistence.Features.Shared.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ST.Tests.Architecture.Database;

[Trait("Category", "Guardrail")]
public class MigrationGuardrailTests
{
    [Fact]
    public void IamSqlServer_ShouldNotHavePendingModelChanges()
    {
        AssertNoPendingModelChanges<IamSqlServerDBContext>(options => options.UseSqlServer("Server=.;Database=dummy;Integrated Security=true"));
    }

    [Fact]
    public void IamPostgreSql_ShouldNotHavePendingModelChanges()
    {
        AssertNoPendingModelChanges<IamPostgreSqlDBContext>(options => options.UseNpgsql("Host=localhost;Database=dummy;Username=postgres;Password=postgres"));
    }

    [Fact]
    public void SharedSqlServer_ShouldNotHavePendingModelChanges()
    {
        AssertNoPendingModelChanges<SharedSqlServerDBContext>(options => options.UseSqlServer("Server=.;Database=dummy;Integrated Security=true"));
    }

    [Fact]
    public void SharedPostgreSql_ShouldNotHavePendingModelChanges()
    {
        AssertNoPendingModelChanges<SharedPostgreSqlDBContext>(options => options.UseNpgsql("Host=localhost;Database=dummy;Username=postgres;Password=postgres"));
    }



    [Fact]
    public void ControlPlaneSqlServer_ShouldNotHavePendingModelChanges()
    {
        AssertNoPendingModelChanges<ControlPlaneSqlServerDBContext>(options => options.UseSqlServer("Server=.;Database=dummy;Integrated Security=true"));
    }

    [Fact]
    public void ControlPlanePostgreSql_ShouldNotHavePendingModelChanges()
    {
        AssertNoPendingModelChanges<ControlPlanePostgreSqlDBContext>(options => options.UseNpgsql("Host=localhost;Database=dummy;Username=postgres;Password=postgres"));
    }

    private static void AssertNoPendingModelChanges<TContext>(Action<DbContextOptionsBuilder> configureOptions) where TContext : DbContext
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddDbContext<TContext>(configureOptions);

        // Register mock providers to satisfy constructor dependencies for various DbContexts
        services.AddScoped<ICurrentTenantProvider, DummyTenantProvider>();
        services.AddScoped<ICurrentActorProvider, DummyActorProvider>();
        // Add a generic logger just in case
        services.AddLogging();

        using var provider = services.BuildServiceProvider();
        using var context = provider.GetRequiredService<TContext>();

        bool hasPendingChanges = context.Database.HasPendingModelChanges();

        Assert.False(hasPendingChanges, $"The model for {typeof(TContext).Name} has changed since the last migration. Please run 'dotnet ef migrations add' to generate a new migration.");
    }

    private sealed class DummyTenantProvider : ICurrentTenantProvider
    {
        public Guid TenantId => Guid.Empty;
    }

    private sealed class DummyActorProvider : ICurrentActorProvider
    {
        public string ActorId => "System";
    }
}
