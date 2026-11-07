using MassTransit.EntityFrameworkCoreIntegration;
using ST.Persistence.Features.ControlPlane.DataContext;
using ST.Persistence.Features.IAM.DataContext;
using ST.Persistence.Features.Shared.DataContext;
using ST.Tests.Integration.TestFixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using Npgsql;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Shared.Contracts.Common;
using System.Text.RegularExpressions;

namespace ST.Tests.Integration;

public abstract class FreshStarterMigrationTests<TFixture>(TFixture fixture) : IClassFixture<TFixture>
    where TFixture : DbFixture
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AllRetainedContextsMigrateAndRerunIdempotently(bool upgrade)
    {
        bool postgres = fixture is PostgreSqlDbFixture;
        string connection = fixture.GetConnectionString();
        var database = upgrade ? "StarterUpgrade" : "StarterCertification";
        if (postgres)
        {
            connection = new NpgsqlConnectionStringBuilder(connection) { Database = database }.ConnectionString;
        }
        else
        {
            var builder = new SqlConnectionStringBuilder(connection) { InitialCatalog = database };
            connection = builder.ConnectionString;
        }
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ICurrentTenantProvider, MigrationTenantProvider>();
        services.AddSingleton<ICurrentActorProvider, MigrationActorProvider>();
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
        var outboxId = Guid.CreateVersion7();
        foreach (var type in contexts)
        {
            var context = (DbContext)scope.ServiceProvider.GetRequiredService(type);
            Assert.False(context.Database.HasPendingModelChanges());
            var previous = context.Database.GetMigrations().LastOrDefault(id => id.EndsWith("_WorkspaceOnboardingAndSupport", StringComparison.Ordinal)
                || (context is SharedDBContext && id.EndsWith("_InitialStarter", StringComparison.Ordinal)));
            if (upgrade && previous is not null)
            {
                await context.GetService<IMigrator>().MigrateAsync(previous);
                if (context is ControlPlaneDBContext)
                {
                    await context.Database.ExecuteSqlRawAsync("UPDATE \"Tenants\" SET \"DisplayName\" = 'Existing customer record' WHERE \"Identifier\" = 'default'");
                }
                if (context is SharedDBContext)
                {
                    await context.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO \"OutboxState\" (\"OutboxId\", \"LockId\", \"Created\", \"BusName\") VALUES ({outboxId}, {outboxId}, {DateTime.UtcNow}, {"legacy"})");
                }
            }

            await context.Database.MigrateAsync();
            if (upgrade && context is ControlPlaneDBContext control)
            {
                var retained = await control.Tenants.SingleAsync(tenant => tenant.Identifier == "default");
                Assert.Equal("Existing customer record", retained.DisplayName);
                Assert.Null(retained.KycApprovedAt);
            }

            if (upgrade && context is SharedDBContext)
            {
                Assert.True(await context.Set<OutboxState>().AnyAsync(state => state.OutboxId == outboxId));
                var busNames = await context.Database.SqlQuery<string>(
                    $"SELECT \"BusName\" AS \"Value\" FROM \"OutboxState\" WHERE \"OutboxId\" = {outboxId}").ToListAsync();
                Assert.Equal("legacy", Assert.Single(busNames));
            }

            Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync());
            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
            await context.Database.MigrateAsync();
            string script = context.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
            Assert.Contains("InitialStarter", script, StringComparison.Ordinal);
            var batches = postgres ? new[] { script } : Regex.Split(script, @"^GO\s*$", RegexOptions.Multiline);
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
        var original = await first.Set<Tenant>().FirstAsync();
        var stale = await second.Set<Tenant>().SingleAsync(tenant => tenant.Id == original.Id);
        byte[] initialVersion = original.RowVersion.ToArray();
        original.DisplayName = "Concurrency winner";
        await first.SaveChangesAsync();
        Assert.False(initialVersion.SequenceEqual(original.RowVersion));
        stale.DisplayName = "Stale update";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    private sealed class MigrationTenantProvider : ICurrentTenantProvider
    {
        public Guid TenantId => Guid.Empty;
    }
    private sealed class MigrationActorProvider : ICurrentActorProvider
    {
        public string ActorId => "MigrationCertification";
    }

}
