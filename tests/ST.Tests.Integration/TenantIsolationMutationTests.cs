using ST.Domain.Features.IAM.Menus.Entities;
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
