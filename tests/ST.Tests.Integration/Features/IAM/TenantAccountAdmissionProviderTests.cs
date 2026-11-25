using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Auditing.Contracts.Repositories;
using ST.Persistence.Features.ControlPlane;
using ST.Persistence.Features.ControlPlane.Tenants.Repositories;
using ST.Infrastructure.Features.ControlPlane.Kyc;
using ST.Infrastructure.Configuration;
using ST.Domain.Features.ControlPlane.Kyc.Entities;
using ST.Domain.Features.ControlPlane.Kyc.Enums;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Persistence.Features.ControlPlane.DataContext;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ST.Domain.Features.IAM.Users.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.IAM.DataContext;
using ST.Persistence.Features.IAM.Users.Stores;
using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration.Features.IAM;

public abstract class TenantAccountAdmissionProviderTests<TFixture>(TFixture fixture) : IClassFixture<TFixture> where TFixture : DbFixture
{
    [Fact]
    public async Task StorageBoundaryEnforcesTenantUniquenessQuotaAndTransactionalAdmission()
    {
        var database = $"Admission_{Guid.CreateVersion7():N}";
        var connection = TestDatabaseProvider.WithDatabase(fixture, database);

        IamDBContext Context(Guid tenant)
        {
            // <db:PostgreSql>
            if (fixture is PostgreSqlDbFixture)
            {
                return new IamPostgreSqlDBContext(new DbContextOptionsBuilder<IamPostgreSqlDBContext>().UseNpgsql(connection).Options, TimeProvider.System, new TenantScope(tenant));
            }
            // </db:PostgreSql>
            // <db:SqlServer>
            if (fixture is MsSqlDbFixture)
            {
                return new IamSqlServerDBContext(new DbContextOptionsBuilder<IamSqlServerDBContext>().UseSqlServer(connection).Options, TimeProvider.System, new TenantScope(tenant));
            }
            // </db:SqlServer>
            throw TestDatabaseProvider.Unsupported(fixture);
        }
        var tenant = Guid.CreateVersion7();
        var limits = new Limits { Value = 1 };

        await using (var db = Context(tenant))
        {
            await db.Database.MigrateAsync();
        }

        async Task<IdentityResult> Add(Guid workspace, string name, bool proxy = false, bool deleted = false)
        {
            await using var db = Context(workspace);
            using var store = new TenantUserStore(db, limits, new IdentityErrorDescriber());
            var user = User(workspace, name);
            user.IsDeleted = deleted;

            if (proxy)
            {
                user.MarkAsSupportProxy();
            }

            return await store.CreateAsync(user);
        }
        var races = await Task.WhenAll(Add(tenant, "one"), Add(tenant, "two"));
        Assert.Single(races.Where(item => item.Succeeded));
        Assert.Contains(races, item => item.Errors.Any(error => error.Code == "TenantAccountLimitReached"));
        // Inactive accounts still occupy a seat; a trusted proxy does not.
        Assert.True((await Add(tenant, "support-proxy", proxy: true)).Succeeded);
        Assert.False((await Add(tenant, "demo-admin-human")).Succeeded);
        limits.Value = 0;
        Assert.True((await Add(tenant, "same")).Succeeded);
        Assert.True((await Add(Guid.CreateVersion7(), "same")).Succeeded);
        Assert.False((await Add(tenant, "same")).Succeeded);
        Assert.True((await Add(tenant, "reserved", deleted: true)).Succeeded);
        Assert.False((await Add(tenant, "reserved")).Succeeded);
        var emailUser = User(tenant, "different-name");
        emailUser.NormalizedEmail = "SAME@EXAMPLE.TEST";

        await using (var db = Context(tenant))
        using (var store = new TenantUserStore(db, limits, new IdentityErrorDescriber()))
        {
            Assert.False((await store.CreateAsync(emailUser)).Succeeded);
        }
        // A caller-owned transaction retains its lock through the final commit.
        var transactionalTenant = Guid.CreateVersion7();
        limits.Value = 1;

        await using (var owner = Context(transactionalTenant))
        {
            await using var transaction = await owner.Database.BeginTransactionAsync();
            using var store = new TenantUserStore(owner, limits, new IdentityErrorDescriber());
            Assert.True((await store.CreateAsync(User(transactionalTenant, "transaction-owner"))).Succeeded);
            var contender = Add(transactionalTenant, "transaction-contender");
            Assert.NotSame(contender, await Task.WhenAny(contender, Task.Delay(200)));
            await transaction.CommitAsync();
            Assert.Contains((await contender).Errors, x => x.Code == "TenantAccountLimitReached");
        }
        var fullTenant = Guid.CreateVersion7();
        limits.Value = 1;
        Assert.True((await Add(fullTenant, "occupied")).Succeeded);
        Assert.True((await Add(fullTenant, "restore", deleted: true)).Succeeded);

        await using (var db = Context(fullTenant))
        using (var store = new TenantUserStore(db, limits, new IdentityErrorDescriber()))
        {
            var restore = await db.Users.IgnoreQueryFilters().SingleAsync(x => x.TenantId == fullTenant && x.UserName == "restore");
            restore.IsDeleted = false;
            Assert.Contains((await store.UpdateAsync(restore)).Errors, x => x.Code == "TenantAccountLimitReached");
        }
        var rollbackTenant = Guid.CreateVersion7();

        await using (var db = Context(rollbackTenant))
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            using var store = new TenantUserStore(db, limits, new IdentityErrorDescriber());
            Assert.True((await store.CreateAsync(User(rollbackTenant, "rolled-back"))).Succeeded);
            await transaction.RollbackAsync();
        }
        Assert.True((await Add(rollbackTenant, "replacement")).Succeeded);
        limits.Value = null;
        Assert.Contains((await Add(Guid.CreateVersion7(), "missing-workspace")).Errors, x => x.Code == "TenantUnavailable");

        await using (var db = Context(tenant))
        using (var store = new TenantUserStore(db, limits, new IdentityErrorDescriber()))
        {
            Assert.False((await store.CreateAsync(User(Guid.CreateVersion7(), "foreign"))).Succeeded);
        }
        // Upgrade existing databases: only paired trusted proxy markers change seat kind.
        limits.Value = 0;
        var upgradeTenant = Guid.CreateVersion7();
        var grant = Guid.CreateVersion7();
        var proxy = User(upgradeTenant, $"support-{grant:N}");
        proxy.Email = $"{proxy.UserName}@example.invalid";
        proxy.NormalizedEmail = proxy.Email.ToUpperInvariant();
        proxy.CreatedBy = "trusted-actor";
        var human = User(upgradeTenant, "support-human");

        await using (var db = Context(upgradeTenant))
        {
            db.Users.AddRange(proxy, human);
            db.UserClaims.AddRange(new IdentityUserClaim<string> { UserId = proxy.Id, ClaimType = "support_actor_id", ClaimValue = "trusted-actor" },
                new IdentityUserClaim<string> { UserId = proxy.Id, ClaimType = "support_grant_id", ClaimValue = grant.ToString() });

            await db.SaveChangesAsync();
            var migrations = db.Database.GetMigrations().ToArray();
            var previous = migrations[Array.FindIndex(migrations, x => x.EndsWith("_EnforceTenantAccountAdmission", StringComparison.Ordinal)) - 1];
            await db.GetService<IMigrator>().MigrateAsync(previous);
            await db.Database.MigrateAsync();
            db.ChangeTracker.Clear();
            Assert.Equal(AccountKind.SupportProxy, (await db.Users.SingleAsync(x => x.Id == proxy.Id)).Kind);
            Assert.Equal(AccountKind.Human, (await db.Users.SingleAsync(x => x.Id == human.Id)).Kind);
            // Legacy duplicate identities stop the upgrade; no account is silently merged or deleted.
            await db.GetService<IMigrator>().MigrateAsync(previous);

            // <db:PostgreSql>
            if (fixture is PostgreSqlDbFixture)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Users\" SET \"NormalizedEmail\" = {human.NormalizedEmail} WHERE \"Id\" = {proxy.Id}");
            }
            // </db:PostgreSql>
            // <db:SqlServer>
            if (fixture is MsSqlDbFixture)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Users] SET [NormalizedEmail] = {human.NormalizedEmail} WHERE [Id] = {proxy.Id}");
            }
            // </db:SqlServer>

            await Assert.ThrowsAnyAsync<Exception>(() => db.Database.MigrateAsync());

            // <db:PostgreSql>
            if (fixture is PostgreSqlDbFixture)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Users\" SET \"NormalizedEmail\" = {proxy.NormalizedEmail} WHERE \"Id\" = {proxy.Id}");
            }
            // </db:PostgreSql>
            // <db:SqlServer>
            if (fixture is MsSqlDbFixture)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [Users] SET [NormalizedEmail] = {proxy.NormalizedEmail} WHERE [Id] = {proxy.Id}");
            }
            // </db:SqlServer>

            await db.Database.MigrateAsync();
            Assert.Equal(2, await db.Users.CountAsync());
            Assert.False(db.Database.HasPendingModelChanges());
        }

    }

    [Fact]
    public async Task ExistingEvidenceIsQuarantinedAndScanLeasesAreConcurrentOnTheProvider()
    {
        var database = $"KycSafety_{Guid.CreateVersion7():N}";
        var connection = TestDatabaseProvider.WithDatabase(fixture, database);

        ControlPlaneDBContext Context()
        {
            // <db:PostgreSql>
            if (fixture is PostgreSqlDbFixture)
            {
                return new ControlPlanePostgreSqlDBContext(new DbContextOptionsBuilder<ControlPlanePostgreSqlDBContext>().UseNpgsql(connection).Options, TimeProvider.System, new Actor());
            }
            // </db:PostgreSql>
            // <db:SqlServer>
            if (fixture is MsSqlDbFixture)
            {
                return new ControlPlaneSqlServerDBContext(new DbContextOptionsBuilder<ControlPlaneSqlServerDBContext>().UseSqlServer(connection).Options, TimeProvider.System, new Actor());
            }
            // </db:SqlServer>
            throw TestDatabaseProvider.Unsupported(fixture);
        }
        var now = TimeProvider.System.GetUtcNow();
        var tenantId = Guid.CreateVersion7();
        var pending = new KycDocument(tenantId, "Registration", "synthetic.pdf", "application/pdf", 10, new string('A', 64), "test", now);
        var erased = new KycDocument(tenantId, "Registration", "erased.pdf", "application/pdf", 10, new string('B', 64), "test", now);
        erased.EraseMetadata(now);

        await using (var db = Context())
        {
            await db.Database.MigrateAsync();
            db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Identifier = $"scan-{tenantId:N}",
                DisplayName = "Scan fixture",
                HostName = $"{tenantId:N}.invalid",
                DeploymentStampId = Tenant.PlatformTenantId,
                MaxUsers = 10,
                CreatedBy = "test"
            });

            db.KycDocuments.AddRange(pending, erased);
            await db.SaveChangesAsync();
            var migrations = db.Database.GetMigrations().ToArray();
            var previous = migrations[Array.FindIndex(migrations, x => x.EndsWith("_QuarantineKycEvidence", StringComparison.Ordinal)) - 1];
            await db.GetService<IMigrator>().MigrateAsync(previous);
            await db.Database.MigrateAsync();
            db.ChangeTracker.Clear();
            var restored = await db.KycDocuments.SingleAsync(x => x.Id == pending.Id);
            Assert.Equal(KycDocumentSafetyStatus.Pending, restored.SafetyStatus);
            Assert.Equal(DateTimeOffset.UnixEpoch, restored.NextSafetyScanAt);
            var erasedState = await db.KycDocuments.SingleAsync(x => x.Id == erased.Id);
            Assert.Equal(KycDocumentSafetyStatus.Erased, erasedState.SafetyStatus);
            Assert.Null(erasedState.NextSafetyScanAt);
            Assert.False(db.Database.HasPendingModelChanges());
        }
        var services = new ServiceCollection();
        services.AddScoped<IControlPlaneUnitOfWork>(_ =>
        {
            var db = Context();
            return new ControlPlaneUnitOfWork(db, new TenantRepository(db), new DeploymentStampRepository(db), Substitute.For<IImpersonationRecordRepository>());
        });

        await using (var provider = services.BuildServiceProvider())
        {
            var processor = new KycDocumentSafetyProcessor(provider.GetRequiredService<IServiceScopeFactory>(),
                Options.Create(new KycDocumentSafetySettings()), Options.Create(new KycDocumentStorageSettings()),
                Options.Create(new OrgSettings { DeploymentStampId = Guid.CreateVersion7() }), TimeProvider.System,
                NullLogger<KycDocumentSafetyProcessor>.Instance);

            Assert.Equal(0, await processor.ProcessBatchAsync(default));
        }
        await using var first = Context();
        await using var second = Context();
        var winner = await first.KycDocuments.SingleAsync(x => x.Id == pending.Id);
        var stale = await second.KycDocuments.SingleAsync(x => x.Id == pending.Id);
        Assert.True(winner.TryBeginSafetyScan(Guid.CreateVersion7(), now, TimeSpan.FromMinutes(3), 5));
        Assert.True(stale.TryBeginSafetyScan(Guid.CreateVersion7(), now, TimeSpan.FromMinutes(3), 5));
        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    private sealed class Actor : ICurrentActorProvider
    {
        public string ActorId => ICurrentActorProvider.SystemActor;
    }

    private static AppUser User(Guid tenant, string name)
    {
        var user = AppUser.CreateSystemUser(TimeProvider.System.GetUtcNow(), tenant, name, $"{name}@example.test", "admission-test");
        user.NormalizedUserName = name.ToUpperInvariant();
        user.NormalizedEmail = user.Email!.ToUpperInvariant();
        return user;
    }

    private sealed class TenantScope(Guid id) : ICurrentTenantProvider
    {
        public Guid TenantId => id;
    }
    private sealed class Limits : ITenantAccountLimitProvider
    {
        public int? Value
        {
            get; set;
        }
        public Task<int?> GetLimitAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Value);
        }
    }
}
