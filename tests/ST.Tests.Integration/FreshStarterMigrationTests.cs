using ST.Persistence.Features.ControlPlane.DataContext;
using ST.Persistence.Features.IAM.DataContext;
using ST.Persistence.Features.Shared.DataContext;
using ST.Tests.Integration.TestFixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace ST.Tests.Integration;

public abstract class FreshStarterMigrationTests<TFixture>(TFixture fixture) : IClassFixture<TFixture>
    where TFixture : DbFixture
{
    [Fact]
    public async Task AllRetainedContextsMigrateAnEmptyDatabaseAndRerunIdempotently()
    {
        bool postgres = fixture is PostgreSqlDbFixture;
        string connection = fixture.GetConnectionString();
        if (!postgres)
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection) { InitialCatalog = "StarterCertification" };
            connection = builder.ConnectionString;
        }
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ST.Domain.Shared.Contracts.Common.ICurrentTenantProvider, MigrationTenantProvider>();
        services.AddSingleton<ST.Domain.Shared.Contracts.Common.ICurrentActorProvider, MigrationActorProvider>();
        void Configure(DbContextOptionsBuilder options, string context)
        {
            if (postgres) options.UseNpgsql(connection, provider => provider.MigrationsHistoryTable("__EFMigrationsHistory_" + context));
            else options.UseSqlServer(connection, provider => provider.MigrationsHistoryTable("__EFMigrationsHistory_" + context));
        }
        if (postgres)
        {
            services.AddDbContext<ControlPlanePostgreSqlDBContext>(options => Configure(options, "ControlPlane"));
            services.AddDbContext<IamPostgreSqlDBContext>(options => Configure(options, "IAM"));
            services.AddDbContext<SharedPostgreSqlDBContext>(options => Configure(options, "Shared"));
        }
        else
        {
            services.AddDbContext<ControlPlaneSqlServerDBContext>(options => Configure(options, "ControlPlane"));
            services.AddDbContext<IamSqlServerDBContext>(options => Configure(options, "IAM"));
            services.AddDbContext<SharedSqlServerDBContext>(options => Configure(options, "Shared"));
        }
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Type[] contexts = postgres
            ? [typeof(ControlPlanePostgreSqlDBContext), typeof(IamPostgreSqlDBContext), typeof(SharedPostgreSqlDBContext)]
            : [typeof(ControlPlaneSqlServerDBContext), typeof(IamSqlServerDBContext), typeof(SharedSqlServerDBContext)];
        foreach (var type in contexts)
        {
            var context = (DbContext)scope.ServiceProvider.GetRequiredService(type);
            Assert.False(context.Database.HasPendingModelChanges());
            await context.Database.MigrateAsync();
            Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync());
            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
            await context.Database.MigrateAsync();
            string script = context.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
            Assert.Contains("InitialStarter", script, StringComparison.Ordinal);
            var batches = postgres ? new[] { script } : System.Text.RegularExpressions.Regex.Split(script, @"^GO\s*$", System.Text.RegularExpressions.RegexOptions.Multiline);
            foreach (var batch in batches.Where(batch => !string.IsNullOrWhiteSpace(batch)))
                await context.Database.ExecuteSqlRawAsync(batch);
            Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync());
        }
        await VerifyConcurrencyAsync(provider, contexts[0]);
    }
    private static async Task VerifyConcurrencyAsync(ServiceProvider provider, Type contextType)
    {
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var first = (DbContext)firstScope.ServiceProvider.GetRequiredService(contextType);
        var second = (DbContext)secondScope.ServiceProvider.GetRequiredService(contextType);
        var original = await first.Set<ST.Domain.Features.ControlPlane.Tenants.Entities.Tenant>().FirstAsync();
        var stale = await second.Set<ST.Domain.Features.ControlPlane.Tenants.Entities.Tenant>().SingleAsync(tenant => tenant.Id == original.Id);
        byte[] initialVersion = original.RowVersion.ToArray();
        original.DisplayName = "Concurrency winner";
        await first.SaveChangesAsync();
        Assert.False(initialVersion.SequenceEqual(original.RowVersion));
        stale.DisplayName = "Stale update";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    private sealed class MigrationTenantProvider : ST.Domain.Shared.Contracts.Common.ICurrentTenantProvider
    {
        public Guid TenantId => Guid.Empty;
    }
    private sealed class MigrationActorProvider : ST.Domain.Shared.Contracts.Common.ICurrentActorProvider
    {
        public string ActorId => "MigrationCertification";
    }

}
