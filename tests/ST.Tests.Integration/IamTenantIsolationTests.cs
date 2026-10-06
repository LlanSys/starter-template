using System;
using System.Threading.Tasks;
using ST.Domain.Features.IAM.Menus.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.IAM.DataContext;
using ST.Tests.Integration.TestFixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ST.Tests.Integration;

public abstract class IamTenantIsolationTests<TFixture> : IClassFixture<TFixture> where TFixture : DbFixture
{
    private readonly DbContextOptions<IamDBContext> _options;
    private readonly TFixture _fixture;

    protected IamTenantIsolationTests(TFixture fixture)
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

    private class TestTenantProvider : ICurrentTenantProvider
    {
        public Guid TenantId { get; set; }
    }

    private class TestActorProvider : ICurrentActorProvider
    {
        public string ActorId => "TestActor";
    }

    [Fact]
    public async Task Query_ShouldOnlyReturnDataForCurrentTenant()
    {
        // Arrange
        var tenant1 = Guid.CreateVersion7();
        var tenant2 = Guid.CreateVersion7();

        var tenantProvider = new TestTenantProvider { TenantId = tenant1 };
        var actorProvider = new TestActorProvider();

        // Seed data acting as tenant 1
        using (var context = new IamDBContext(_options, tenantProvider, actorProvider))
        {
            await context.Database.EnsureCreatedAsync();

            var menu1 = MenuItem.Create(null, $"menu-1-{tenant1}", "Menu 1", "Desc 1", "/m1", "icon1", "top", null, null, 1, "test");
            context.MenuItems.Add(menu1);
            await context.SaveChangesAsync();
        }

        // Seed data acting as tenant 2
        tenantProvider.TenantId = tenant2;
        using (var context = new IamDBContext(_options, tenantProvider, actorProvider))
        {
            var menu2 = MenuItem.Create(null, $"menu-2-{tenant2}", "Menu 2", "Desc 2", "/m2", "icon2", "top", null, null, 2, "test");
            context.MenuItems.Add(menu2);
            await context.SaveChangesAsync();
        }

        // Act - Query as tenant 1
        tenantProvider.TenantId = tenant1;
        using (var context = new IamDBContext(_options, tenantProvider, actorProvider))
        {
            var items = await context.MenuItems.ToListAsync();

            // Assert
            Assert.Contains(items, i => i.Key == $"menu-1-{tenant1}");
            Assert.DoesNotContain(items, i => i.Key == $"menu-2-{tenant2}");
        }

        // Act - Query as tenant 2
        tenantProvider.TenantId = tenant2;
        using (var context = new IamDBContext(_options, tenantProvider, actorProvider))
        {
            var items = await context.MenuItems.ToListAsync();

            // Assert
            Assert.Contains(items, i => i.Key == $"menu-2-{tenant2}");
            Assert.DoesNotContain(items, i => i.Key == $"menu-1-{tenant1}");
        }
    }

    [Fact]
    public async Task IgnoreQueryFilters_ShouldReturnDataForAllTenants()
    {
        // Arrange
        var tenant1 = Guid.CreateVersion7();
        var tenant2 = Guid.CreateVersion7();

        var tenantProvider = new TestTenantProvider { TenantId = tenant1 };
        var actorProvider = new TestActorProvider();

        // Seed data acting as tenant 1
        using (var context = new IamDBContext(_options, tenantProvider, actorProvider))
        {
            await context.Database.EnsureCreatedAsync();

            var menu1 = MenuItem.Create(null, $"menu-3-{tenant1}", "Menu 3", "Desc 1", "/m3", "icon1", "top", null, null, 1, "test");
            context.MenuItems.Add(menu1);
            await context.SaveChangesAsync();
        }

        // Seed data acting as tenant 2
        tenantProvider.TenantId = tenant2;
        using (var context = new IamDBContext(_options, tenantProvider, actorProvider))
        {
            var menu2 = MenuItem.Create(null, $"menu-4-{tenant2}", "Menu 4", "Desc 2", "/m4", "icon2", "top", null, null, 2, "test");
            context.MenuItems.Add(menu2);
            await context.SaveChangesAsync();
        }

        // Act - Ignore query filters
        tenantProvider.TenantId = tenant1; // Doesn't matter for this query
        using (var context = new IamDBContext(_options, tenantProvider, actorProvider))
        {
            var items = await context.MenuItems.IgnoreQueryFilters().ToListAsync();

            // Assert
            Assert.Contains(items, i => i.Key == $"menu-3-{tenant1}");
            Assert.Contains(items, i => i.Key == $"menu-4-{tenant2}");
        }
    }
}
