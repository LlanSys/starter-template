using ST.Domain.Features.HR.Departments.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.HR.DataContext;
using ST.Tests.Integration.TestFixtures;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ST.Tests.Integration;

public abstract class HrTenantIsolationTests<TFixture> : IClassFixture<TFixture> where TFixture : DbFixture
{
    private readonly DbContextOptions<HrDBContext> _options;
    private readonly TFixture _fixture;

    protected HrTenantIsolationTests(TFixture fixture)
    {
        _fixture = fixture;

        var builder = new DbContextOptionsBuilder<HrDBContext>();

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
        using (var context = new HrDBContext(_options, tenantProvider, actorProvider))
        {
            await context.Database.EnsureCreatedAsync();

            var dept1 = Department.Create("DEPT1", "Department 1", "Desc 1", "test");
            context.Departments.Add(dept1);
            await context.SaveChangesAsync();
        }

        // Seed data acting as tenant 2
        tenantProvider.TenantId = tenant2;
        using (var context = new HrDBContext(_options, tenantProvider, actorProvider))
        {
            var dept2 = Department.Create("DEPT2", "Department 2", "Desc 2", "test");
            context.Departments.Add(dept2);
            await context.SaveChangesAsync();
        }

        // Act - Query as tenant 1
        tenantProvider.TenantId = tenant1;
        using (var context = new HrDBContext(_options, tenantProvider, actorProvider))
        {
            var items = await context.Departments.ToListAsync();

            // Assert
            Assert.Contains(items, i => i.Code == "DEPT1");
            Assert.DoesNotContain(items, i => i.Code == "DEPT2");
        }

        // Act - Query as tenant 2
        tenantProvider.TenantId = tenant2;
        using (var context = new HrDBContext(_options, tenantProvider, actorProvider))
        {
            var items = await context.Departments.ToListAsync();

            // Assert
            Assert.Contains(items, i => i.Code == "DEPT2");
            Assert.DoesNotContain(items, i => i.Code == "DEPT1");
        }
    }
}
