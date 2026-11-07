using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.IAM.DataContext;

namespace ST.Tests.Unit;
public sealed class TenantRoleIsolationTests
{
    [Fact]
    public async Task TenantsCanUseTheSameRoleNameWithoutSeeingEachOthersRole()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<RoleTestContext>().UseSqlite(connection).Options;
        var firstTenant = new TenantProvider(Guid.CreateVersion7());
        var secondTenant = new TenantProvider(Guid.CreateVersion7());
        await using (var first = new RoleTestContext(options, firstTenant))
        {
            await first.Database.EnsureCreatedAsync();
            first.Roles.Add(new AppRole { Id = Guid.CreateVersion7().ToString(), Name = "Manager", NormalizedName = "MANAGER" });
            await first.SaveChangesAsync();
        }
        await using (var second = new RoleTestContext(options, secondTenant))
        {
            Assert.Empty(await second.Roles.ToListAsync());
            second.Roles.Add(new AppRole { Id = Guid.CreateVersion7().ToString(), Name = "Manager", NormalizedName = "MANAGER" });
            await second.SaveChangesAsync();
            Assert.Equal(secondTenant.TenantId, (await second.Roles.SingleAsync()).TenantId);
            Assert.Equal(2, await second.Roles.IgnoreQueryFilters().CountAsync());
        }
        await using var again = new RoleTestContext(options, firstTenant);
        Assert.Equal(firstTenant.TenantId, (await again.Roles.SingleAsync()).TenantId);
    }
    private sealed class TenantProvider(Guid tenantId) : ICurrentTenantProvider { public Guid TenantId => tenantId; }
    private sealed class RoleTestContext(DbContextOptions<RoleTestContext> options, ICurrentTenantProvider tenant) : IamDBContext(options, tenant)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            foreach (var entity in builder.Model.GetEntityTypes())
            {
                var token = entity.FindProperty("RowVersion");
                if (token is not null) token.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
            }
        }
    }
}
