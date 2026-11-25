using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using ST.Domain.Shared;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.ControlPlane.Tenants.Commands;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Contracts.Implementations.Common;
using ST.Persistence.Common.Interceptors;
using ST.Persistence.Features.ControlPlane.DataContext;
using ST.Persistence.Features.ControlPlane.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Features.ControlPlane.Provisioning;
using ST.Persistence.Features.IAM.DataContext;
using ST.Persistence.Features.Shared.DataContext;
using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration;

public abstract class TenantDatabaseReadinessTests<TFixture>(TFixture fixture) : IClassFixture<TFixture>
    where TFixture : DbFixture
{

    [Fact]
    public async Task SuspendedCutoverAndRollbackRouteToVerifiedDataWithoutAffectingTheOtherWorkspace()
    {
        var providerName = TestDatabaseProvider.ProviderOf(fixture);
        var sourceName = "source_" + Guid.CreateVersion7().ToString("N");
        var targetName = "target_" + Guid.CreateVersion7().ToString("N");
        var sourceConnection = TestDatabaseProvider.WithDatabase(fixture, sourceName);
        var targetConnection = TestDatabaseProvider.WithDatabase(fixture, targetName);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = sourceConnection,
            ["DatabaseSettings:Provider"] = providerName
        }).Build();
        var tenantId = Guid.CreateVersion7();
        var otherId = Guid.CreateVersion7();
        var current = Substitute.For<ICurrentTenantProvider>();
        current.TenantId.Returns(tenantId);
        var actor = Substitute.For<ICurrentActorProvider>();
        actor.ActorId.Returns("fixture");
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Development);
        var encryption = Substitute.For<IEncryptionService>();
        encryption.Encrypt(Arg.Any<string>()).Returns(call => "protected:" + call.Arg<string>());
        encryption.Decrypt(Arg.Any<string>()).Returns(call => call.Arg<string>()[10..]);

        var collection = new ServiceCollection().AddLogging()
            .AddSingleton(TimeProvider.System).AddSingleton(current).AddSingleton(actor).AddSingleton(encryption)
            .AddScoped<ITenantDatabaseReadiness, TenantDatabaseReadiness>()
            .AddScoped<ITenantConnectionResolver, TenantConnectionResolver>()
            .AddMediatR(options => options.RegisterServicesFromAssemblyContaining<MigrateTenantStampCommand>());
        collection.AddControlPlanePersistence(configuration, environment);
        await using var services = collection.BuildServiceProvider();
        await using var scope = services.CreateAsyncScope();
        var control = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
        await control.Database.MigrateAsync();
        var targetStamp = new DeploymentStamp
        {
            Name = "isolated",
            TargetResourceGroup = "fixture",
            CreatedBy = "fixture",
            IsolationTier = IsolationTier.Isolated,
            DatabaseProvider = providerName
        };
        control.DeploymentStamps.Add(targetStamp);
        var pooledStamp = await control.DeploymentStamps.SingleAsync();
        pooledStamp.DatabaseProvider = providerName;
        var tenant = new Tenant
        {
            Id = tenantId,
            Identifier = "cutover",
            DisplayName = "Cutover",
            HostName = "cutover.invalid",
            CreatedBy = "fixture",
            DeploymentStampId = pooledStamp.Id,
            DatabaseProvider = providerName,
            DatabaseConnectionString = encryption.Encrypt(sourceConnection)
        };
        tenant.Approve("fixture", DateTimeOffset.UnixEpoch, "review");
        tenant.MarkAsActive();
        var other = new Tenant
        {
            Id = otherId,
            Identifier = "untouched",
            DisplayName = "Untouched",
            HostName = "untouched.invalid",
            CreatedBy = "fixture",
            DeploymentStampId = pooledStamp.Id
        };
        other.Approve("fixture", DateTimeOffset.UnixEpoch, "review");
        other.MarkAsActive();
        control.Tenants.AddRange(tenant, other);
        await control.SaveChangesAsync();

        await using var source = CreateIam(sourceConnection, providerName);
        await using var target = CreateIam(targetConnection, providerName);
        await source.Database.MigrateAsync();
        await target.Database.MigrateAsync();
        await using var shared = CreateShared(targetConnection, providerName);
        await shared.Database.MigrateAsync();
        await using var sourceShared = CreateShared(sourceConnection, providerName);
        await sourceShared.Database.MigrateAsync();
        source.Users.Add(AppUser.CreateSystemUser(DateTimeOffset.UnixEpoch, tenantId, "owner", "owner@example.test", "fixture"));
        source.Users.Add(AppUser.CreateSystemUser(DateTimeOffset.UnixEpoch, otherId, "other", "other@example.test", "fixture"));
        await source.SaveChangesAsync();
        target.Users.Add(await source.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(user => user.TenantId == tenantId));
        await target.SaveChangesAsync();

        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var move = new MigrateTenantStampCommand(tenantId, targetStamp.Id, targetConnection, "https://isolated.example.test/");
        Assert.False((await sender.Send(move)).IsSuccess);
        Assert.Equal(pooledStamp.Id, tenant.DeploymentStampId);
        tenant.Suspend();
        await control.SaveChangesAsync();
        Assert.False((await sender.Send(move with { NewDatabaseConnectionString = sourceConnection })).IsSuccess);
        Assert.Equal(pooledStamp.Id, tenant.DeploymentStampId);
        Assert.True((await sender.Send(move)).IsSuccess);
        Assert.Equal("https://isolated.example.test", tenant.ClientOrigin);
        var competitor = new Tenant
        {
            Identifier = "competitor",
            DisplayName = "Competitor",
            HostName = "competitor.invalid",
            CreatedBy = "fixture",
            DeploymentStampId = targetStamp.Id,
            ExclusiveStampId = targetStamp.Id
        };
        control.Tenants.Add(competitor);
        await Assert.ThrowsAsync<DbUpdateException>(() => control.SaveChangesAsync());
        control.Entry(competitor).State = EntityState.Detached;
        tenant.Reactivate(TimeProvider.System.GetUtcNow());
        await control.SaveChangesAsync();

        await using (var routedScope = services.CreateAsyncScope())
        {
            await using var routed = CreateIam(sourceConnection, providerName, current,
                new TenantConnectionInterceptor(routedScope.ServiceProvider));
            Assert.Equal("owner", (await routed.Users.SingleAsync()).UserName);

            Assert.Equal(targetName, routed.Database.GetDbConnection().Database);
        }
        Assert.Equal("other", (await source.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(user => user.TenantId == otherId)).UserName);
        tenant.Suspend();
        await control.SaveChangesAsync();
        Assert.True((await sender.Send(new MigrateTenantStampCommand(tenantId, pooledStamp.Id, sourceConnection, "https://pooled.example.test"))).IsSuccess);
        tenant.Reactivate(TimeProvider.System.GetUtcNow());
        await control.SaveChangesAsync();
        await using (var routedScope = services.CreateAsyncScope())
        {
            await using var routed = CreateIam(targetConnection, providerName, current,
                new TenantConnectionInterceptor(routedScope.ServiceProvider));
            Assert.Equal("owner", (await routed.Users.SingleAsync()).UserName);
            Assert.Equal(sourceName, routed.Database.GetDbConnection().Database);
        }
        Assert.Equal("https://pooled.example.test", tenant.ClientOrigin);
        Assert.Equal(2, await source.Users.IgnoreQueryFilters().CountAsync());
        Assert.Single(await target.Users.IgnoreQueryFilters().ToArrayAsync());
    }

    private static IamDBContext CreateIam(string connection, string provider,
        ICurrentTenantProvider? tenant = null, TenantConnectionInterceptor? interceptor = null)
    {
        // <db:PostgreSql>
        if (DatabaseProviders.Is(provider, DatabaseProviders.PostgreSql))
        {
            var options = new DbContextOptionsBuilder<IamPostgreSqlDBContext>()
                .UseNpgsql(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_IAM"));
            if (interceptor is not null) { options.AddInterceptors(interceptor); }
            return new IamPostgreSqlDBContext(options.Options, TimeProvider.System, tenant);
        }
        // </db:PostgreSql>
        // <db:SqlServer>
        if (DatabaseProviders.Is(provider, DatabaseProviders.SqlServer))
        {
            var options = new DbContextOptionsBuilder<IamSqlServerDBContext>()
                .UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_IAM"));
            if (interceptor is not null) { options.AddInterceptors(interceptor); }
            return new IamSqlServerDBContext(options.Options, TimeProvider.System, tenant);
        }
        // </db:SqlServer>
        throw new NotSupportedException(provider);
    }

    private static SharedDBContext CreateShared(string connection, string provider)
    {
        // <db:PostgreSql>
        if (DatabaseProviders.Is(provider, DatabaseProviders.PostgreSql))
        {
            return new SharedPostgreSqlDBContext(new DbContextOptionsBuilder<SharedPostgreSqlDBContext>()
                .UseNpgsql(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_Shared")).Options, TimeProvider.System, null!, null!);
        }
        // </db:PostgreSql>
        // <db:SqlServer>
        if (DatabaseProviders.Is(provider, DatabaseProviders.SqlServer))
        {
            return new SharedSqlServerDBContext(new DbContextOptionsBuilder<SharedSqlServerDBContext>()
                .UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_Shared")).Options, TimeProvider.System, null!, null!);
        }
        // </db:SqlServer>
        throw new NotSupportedException(provider);
    }

    [Fact]
    public async Task DestinationRequiresCurrentSchemasAndExcludesAnotherWorkspacesData()
    {
        var database = "readiness_" + Guid.CreateVersion7().ToString("N");
        var connection = TestDatabaseProvider.WithDatabase(fixture, database);
        var provider = TestDatabaseProvider.ProviderOf(fixture);
        var tenant = Guid.CreateVersion7();
        using var logs = LoggerFactory.Create(builder => builder.AddConsole());
        var readiness = new TenantDatabaseReadiness(TimeProvider.System, logs.CreateLogger<TenantDatabaseReadiness>());
        Assert.False((await readiness.VerifyAsync(tenant, connection, provider, CancellationToken.None)).IsSuccess);

        IamDBContext? iamCreated = null;
        // <db:PostgreSql>
        if (fixture is PostgreSqlDbFixture)
        {
            iamCreated = new IamPostgreSqlDBContext(new DbContextOptionsBuilder<IamPostgreSqlDBContext>() .UseNpgsql(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_IAM")).Options, TimeProvider.System);
        }
        // </db:PostgreSql>
        // <db:SqlServer>
        if (fixture is MsSqlDbFixture)
        {
            iamCreated = new IamSqlServerDBContext(new DbContextOptionsBuilder<IamSqlServerDBContext>() .UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_IAM")).Options, TimeProvider.System);
        }
        // </db:SqlServer>
        await using IamDBContext iam = iamCreated ?? throw TestDatabaseProvider.Unsupported(fixture);
        SharedDBContext? sharedCreated = null;
        // <db:PostgreSql>
        if (fixture is PostgreSqlDbFixture)
        {
            sharedCreated = new SharedPostgreSqlDBContext(new DbContextOptionsBuilder<SharedPostgreSqlDBContext>() .UseNpgsql(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_Shared")).Options, TimeProvider.System, null!, null!);
        }
        // </db:PostgreSql>
        // <db:SqlServer>
        if (fixture is MsSqlDbFixture)
        {
            sharedCreated = new SharedSqlServerDBContext(new DbContextOptionsBuilder<SharedSqlServerDBContext>() .UseSqlServer(connection, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory_Shared")).Options, TimeProvider.System, null!, null!);
        }
        // </db:SqlServer>
        await using SharedDBContext shared = sharedCreated ?? throw TestDatabaseProvider.Unsupported(fixture);
        await iam.Database.MigrateAsync();
        Assert.False((await readiness.VerifyAsync(tenant, connection, provider, CancellationToken.None)).IsSuccess);
        await shared.Database.MigrateAsync();
        var verified = await readiness.VerifyAsync(tenant, connection, provider, CancellationToken.None);
        Assert.True(verified.IsSuccess, verified.Message);

        iam.Users.Add(AppUser.CreateSystemUser(DateTimeOffset.UnixEpoch, tenant, "owned", "owned@example.test", "fixture"));
        await iam.SaveChangesAsync();
        Assert.True((await readiness.VerifyAsync(tenant, connection, provider, CancellationToken.None)).IsSuccess);
        iam.Users.Add(AppUser.CreateSystemUser(DateTimeOffset.UnixEpoch, Guid.CreateVersion7(), "foreign", "foreign@example.test", "fixture"));
        await iam.SaveChangesAsync();
        Assert.False((await readiness.VerifyAsync(tenant, connection, provider, CancellationToken.None)).IsSuccess);
        Assert.Equal(2, await iam.Users.IgnoreQueryFilters().CountAsync());
    }
}
