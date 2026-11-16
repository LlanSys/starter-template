using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Features.ControlPlane;
using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration.ControlPlane;

public abstract class PlatformStaffMigrationTests<TFixture>(TFixture fixture) : IClassFixture<TFixture> where TFixture : DbFixture
{
    [Fact]
    public async Task UpgradeGrantsOnlyExplicitFullAuthorityStaffAndCreatesAuditStore()
    {
        var postgres = fixture is PostgreSqlDbFixture;
        var name = $"StaffMigration_{Guid.CreateVersion7():N}";
        var connection = postgres ? new NpgsqlConnectionStringBuilder(fixture.GetConnectionString())
        {
            Database = name
        }.ConnectionString : new SqlConnectionStringBuilder(fixture.GetConnectionString())
        {
            InitialCatalog = name
        }.ConnectionString;
        await using IamDBContext db = postgres ? new IamPostgreSqlDBContext(new DbContextOptionsBuilder<IamPostgreSqlDBContext>().UseNpgsql(connection).Options, TimeProvider.System) : new IamSqlServerDBContext(new DbContextOptionsBuilder<IamSqlServerDBContext>().UseSqlServer(connection).Options, TimeProvider.System);
        var migrations = db.Database.GetMigrations().ToArray();
        var previous = Array.FindIndex(migrations, m => m.EndsWith("_EnforceTenantAccountAdmission", StringComparison.Ordinal));
        await db.GetService<IMigrator>().MigrateAsync(migrations[previous]);
        var cases = new[]
        {
            "full",
            "limited",
            "role-label-only",
            "inactive",
            "foreign",
            "proxy"
        };
        var accounts = cases.ToDictionary(k => k, k => AppUser.CreateSystemUser(
            DateTimeOffset.UnixEpoch,
            k == "foreign" ? Guid.CreateVersion7() : Tenant.PlatformTenantId,
            k,
            k + "@example.test",
            "fixture"
        ));
        foreach (var(kind, user)in accounts)
        {
            user.NormalizedUserName = kind.ToUpperInvariant();
            user.NormalizedEmail = user.Email!.ToUpperInvariant();
            user.IsActive = kind != "inactive";
            user.EmailConfirmed = true;
            if (kind == "proxy")
            {
                user.MarkAsSupportProxy();
            }

            db.Users.Add(user);
            if (kind != "role-label-only")
            {
                db.UserClaims.Add(new IdentityUserClaim<string> { UserId = user.Id, ClaimType = PlatformAccess.StaffClaim, ClaimValue = "true" });
            }

            foreach (var key in PlatformPermissions.All.Where(k => k != PlatformPermissions.ViewPlatformStaff && k != PlatformPermissions.ManagePlatformStaff))
            {
                if (kind == "limited" && key != PlatformPermissions.ViewKyc)
                {
                    continue;
                }

                db.UserClaims.Add(new IdentityUserClaim<string> { UserId = user.Id, ClaimType = "permission", ClaimValue = key });
            }
        }

        var managed = new AppRole
        {
            Id = Guid.CreateVersion7().ToString(),
            TenantId = Tenant.PlatformTenantId,
            Name = "Tenant Administrator",
            NormalizedName = "TENANT ADMINISTRATOR",
            TemplateKey = "iam-administrator"
        };
        var copied = new AppRole
        {
            Id = Guid.CreateVersion7().ToString(),
            TenantId = Tenant.PlatformTenantId,
            Name = "Copied Admin",
            NormalizedName = "COPIED ADMIN",
            TemplateKey = "iam-administrator"
        };
        db.Roles.AddRange(managed, copied);
        await db.SaveChangesAsync();
        await db.Database.MigrateAsync();
        var settingsGrants = await db.RoleClaims.Where(c => c.ClaimType == "permission" && c.ClaimValue!.StartsWith("tenant-settings.")).ToArrayAsync();
        Assert.Equal(4, settingsGrants.Length);
        Assert.All(settingsGrants, c => Assert.Equal(managed.Id, c.RoleId));
        var granted = await db.UserClaims.Where(c => c.ClaimType == "permission" && (c.ClaimValue == PlatformPermissions.ViewPlatformStaff || c.ClaimValue == PlatformPermissions.ManagePlatformStaff)).ToArrayAsync();
        Assert.Equal(2, granted.Length);
        Assert.All(granted, c => Assert.Equal(accounts["full"].Id, c.UserId));
        Assert.Empty(await db.PlatformStaffChanges.ToArrayAsync());
        await using var transaction = await db.Database.BeginTransactionAsync();
        if (postgres)
        {
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(731043, 1)");
        }
        else
        {
            await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'ST:IAM:PlatformStaffAuthority', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=30000; IF @r < 0 THROW 51000, 'Authority lock unavailable', 1;");
        }

        await transaction.RollbackAsync();
    }
}
