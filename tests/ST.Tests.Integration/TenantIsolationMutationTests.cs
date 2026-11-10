using ST.Domain.Features.IAM.Menus.Entities;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.IAM.DataContext;
using ST.Tests.Integration.TestFixtures;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ST.Tests.Integration;

public abstract class TenantIsolationMutationTests<TFixture> : IClassFixture<TFixture> where TFixture : DbFixture
{
    private readonly DbContextOptions<IamDBContext> _options;
    private readonly TFixture _fixture;

    protected TenantIsolationMutationTests(TFixture fixture)
    {
        _fixture = fixture;

        var builder = new DbContextOptionsBuilder<IamDBContext>();

        if (fixture is PostgreSqlDbFixture)
        {
            builder.UseNpgsql(fixture.GetConnectionString());
        }
        else if (fixture is MsSqlDbFixture)
        {
            builder.UseSqlServer(fixture.GetConnectionString());
        }

        _options = builder.Options;
    }

    [Fact]
    public async Task IgnoringQueryFilters_LeaksDataAcrossTenants()
    {
        // Arrange
        using (var initContext = new IamDBContext(_options, TimeProvider.System))
        {
            await initContext.Database.EnsureCreatedAsync();
        }

        var tenantId1 = Guid.NewGuid();
        var tenantId2 = Guid.NewGuid();

        // Seed data for Tenant 1
        using (var seedContext1 = new IamDBContext(_options, TimeProvider.System, new TestTenantProvider { TenantId = tenantId1 }))
        {
            var menu = MenuItem.Create(null, "menu1", "Tenant 1 Menu", "Desc", "/t1", "icon", "left", null, null, 1, "test");
            menu.TenantId = tenantId1;
            seedContext1.MenuItems.Add(menu);
            await seedContext1.SaveChangesAsync();
        }

        // Seed data for Tenant 2
        using (var seedContext2 = new IamDBContext(_options, TimeProvider.System, new TestTenantProvider { TenantId = tenantId2 }))
        {
            var menu = MenuItem.Create(null, "menu2", "Tenant 2 Menu", "Desc", "/t2", "icon", "left", null, null, 1, "test");
            menu.TenantId = tenantId2;
            seedContext2.MenuItems.Add(menu);
            await seedContext2.SaveChangesAsync();
        }

        // Act & Assert
        // Tenant 1 should normally only see Tenant 1's data
        using (var testContext = new IamDBContext(_options, TimeProvider.System, new TestTenantProvider { TenantId = tenantId1 }))
        {
            var normallyVisible = await testContext.MenuItems.CountAsync();
            Assert.Equal(1, normallyVisible);

            // MUTATION TEST: Deliberately bypass the global query filter
            var rawVisible = await testContext.MenuItems.IgnoreQueryFilters().CountAsync();

            // The test suite catches the leak. This proves the query filter is actually doing the work.
            Assert.True(rawVisible >= 2, "Ignoring query filters should expose data from other tenants.");
        }
    }

    [Fact]
    public async Task DetachedWritesCannotUseAnotherTenantsKeyOrChangeItsOwnership()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        await using var owner = new IamDBContext(_options, TimeProvider.System, new TestTenantProvider { TenantId = tenantB });
        await owner.Database.EnsureCreatedAsync();
        var original = MenuItem.Create(null, "owned", "Owned by B", "Description", "/owned", "icon", "left", null, null, 1, "test");
        owner.MenuItems.Add(original);
        await owner.SaveChangesAsync();

        await using (var wrong = new IamDBContext(_options, TimeProvider.System, new TestTenantProvider { TenantId = tenantA }))
        {
            var foreign = MenuItem.Create(null, "foreign", "Foreign insert", "Description", "/foreign", "icon", "left", null, null, 1, "test");
            foreign.TenantId = tenantB;
            wrong.MenuItems.Add(foreign);
            await Assert.ThrowsAsync<InvalidOperationException>(() => wrong.SaveChangesAsync());
        }

        var detached = await owner.MenuItems.AsNoTracking().SingleAsync(menu => menu.Id == original.Id);
        detached.TenantId = tenantA;
        await using (var wrong = new IamDBContext(_options, TimeProvider.System, new TestTenantProvider { TenantId = tenantA }))
        {
            wrong.MenuItems.Update(detached);
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => wrong.SaveChangesAsync());
        }

        original.TenantId = tenantA;
        await Assert.ThrowsAsync<InvalidOperationException>(() => owner.SaveChangesAsync());
        owner.ChangeTracker.Clear();
        Assert.Equal(tenantB, (await owner.MenuItems.AsNoTracking().SingleAsync(menu => menu.Id == original.Id)).TenantId);
    }

    [Fact]
    public async Task TenantCannotCreateASessionForAnotherWorkspacesAccount()
    {
        var tenantA = Guid.CreateVersion7();
        var tenantB = Guid.CreateVersion7();
        await using var owner = new IamDBContext(_options, TimeProvider.System, new TestTenantProvider { TenantId = tenantB });
        await owner.Database.EnsureCreatedAsync();
        var now = TimeProvider.System.GetUtcNow();
        var user = AppUser.CreateSystemUser(now, tenantB, $"owner-{tenantB:N}", $"owner-{tenantB:N}@example.test", "test");
        owner.Users.Add(user);
        await owner.SaveChangesAsync();

        await using var foreign = new IamDBContext(_options, TimeProvider.System, new TestTenantProvider { TenantId = tenantA });
        foreign.AppUserSessions.Add(AppUserSession.Create(now, Guid.CreateVersion7(), user.Id, "127.0.0.1",
            "integration", now.AddHours(1), "test"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => foreign.SaveChangesAsync());
    }

    private class TestTenantProvider : ICurrentTenantProvider
    {
        public Guid TenantId { get; set; }
    }

    private class FakeTenantConnectionResolver : ITenantConnectionResolver
    {
        public Guid? CurrentTenantId { get; }
        private readonly string? _connectionString;

        public FakeTenantConnectionResolver(Guid tenantId, string? connectionString = null)
        {
            CurrentTenantId = tenantId;
            _connectionString = connectionString;
        }

        public Task<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_connectionString);

        public Task<string?> GetDatabaseProviderAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }
}
