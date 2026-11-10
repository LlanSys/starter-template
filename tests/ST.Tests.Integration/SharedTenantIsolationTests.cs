using ST.Domain.Features.Shared.OrgSettings.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.Shared.DataContext;
using ST.Tests.Integration.TestFixtures;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ST.Tests.Integration;

public abstract class SharedTenantIsolationTests<TFixture> : IClassFixture<TFixture> where TFixture : DbFixture
{
    private readonly DbContextOptions<SharedDBContext> _options;
    private readonly TFixture _fixture;

    protected SharedTenantIsolationTests(TFixture fixture)
    {
        _fixture = fixture;

        var builder = new DbContextOptionsBuilder<SharedDBContext>();

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
        using (var context = new SharedDBContext(_options, TimeProvider.System, tenantProvider, actorProvider))
        {
            await context.Database.EnsureCreatedAsync();

            var setting1 = new OrgSetting($"SETTING_1_{tenant1}", "Value 1", "test");
            context.OrgSettings.Add(setting1);
            await context.SaveChangesAsync();
        }

        // Seed data acting as tenant 2
        tenantProvider.TenantId = tenant2;
        using (var context = new SharedDBContext(_options, TimeProvider.System, tenantProvider, actorProvider))
        {
            var setting2 = new OrgSetting($"SETTING_2_{tenant2}", "Value 2", "test");
            context.OrgSettings.Add(setting2);
            await context.SaveChangesAsync();
        }

        // Act - Query as tenant 1
        tenantProvider.TenantId = tenant1;
        using (var context = new SharedDBContext(_options, TimeProvider.System, tenantProvider, actorProvider))
        {
            var items = await context.OrgSettings.ToListAsync();

            // Assert
            Assert.Contains(items, i => i.Key == $"SETTING_1_{tenant1}");
            Assert.DoesNotContain(items, i => i.Key == $"SETTING_2_{tenant2}");
        }

        // Act - Query as tenant 2
        tenantProvider.TenantId = tenant2;
        using (var context = new SharedDBContext(_options, TimeProvider.System, tenantProvider, actorProvider))
        {
            var items = await context.OrgSettings.ToListAsync();

            // Assert
            Assert.Contains(items, i => i.Key == $"SETTING_2_{tenant2}");
            Assert.DoesNotContain(items, i => i.Key == $"SETTING_1_{tenant1}");
        }
    }
}
