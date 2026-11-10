using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ST.Domain.Features.IAM.Menus.Entities;
using ST.Persistence.Features.IAM.DataContext;
using Microsoft.EntityFrameworkCore.Metadata;
using ST.Domain.Shared.Contracts.Common;

namespace ST.Tests.Unit;

public sealed class MenuVisibilityTests
{
    private static HashSet<string> Set(params string[] values) => new(values, StringComparer.OrdinalIgnoreCase);
    private static MenuItem Menu(string? legacy = null) => MenuItem.Create(null, "test-menu", "Test", "", "/dashboard", "Menu", "Sidebar", legacy, null, 0, "test-actor");

    [Fact]
    public void RolesAreAnyAndPermissionsAreAll()
    {
        var menu = Menu();
        menu.SetVisibility(["Manager", "Reviewer"], ["users.view", "users.edit"]);
        Assert.True(menu.IsVisibleTo(Set("reviewer"), Set("users.view", "users.edit")));
        Assert.False(menu.IsVisibleTo(Set("Reviewer"), Set("users.view")));
        Assert.False(menu.IsVisibleTo(Set("Other"), Set("users.view", "users.edit")));
    }

    [Fact]
    public void EmptySelectionsAndLegacyPermissionRemainCompatible()
    {
        Assert.True(Menu().IsVisibleTo(Set(), Set()));
        var menu = Menu("users.view");
        Assert.False(menu.IsVisibleTo(Set(), Set()));
        Assert.True(menu.IsVisibleTo(Set(), Set("USERS.VIEW")));
    }

    [Fact]
    public void AdministratorCannotBypassRoleRestrictionsOrControlPlanePermission()
    {
        var menu = Menu();
        menu.SetVisibility(["Reviewer"], ["users.view"]);
        Assert.False(menu.IsVisibleTo(Set("System Administrator"), Set()));
        Assert.False(menu.IsVisibleTo(Set("Reviewer"), Set()));
        menu.SetVisibility([], ["control_plane.manage"]);
        Assert.False(menu.IsVisibleTo(Set("System Administrator"), Set()));
        Assert.True(menu.IsVisibleTo(Set(), Set("control_plane.manage")));
        Assert.False(Menu("control_plane.manage").IsVisibleTo(Set(), Set()));
    }

    [Fact]
    public async Task VisibilitySelectionsRoundTripThroughPersistence()
    {
        await using var context = new SqliteIamDBContext(new DbContextOptionsBuilder<SqliteIamDBContext>().UseSqlite("Data Source=:memory:").Options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();
        var menu = Menu();
        menu.SetVisibility([" Reviewer ", "reviewer", "Manager"], ["users.view", "users.view"]);
        context.Add(menu);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var stored = await context.Set<MenuItem>().IgnoreQueryFilters().SingleAsync(item => item.Id == menu.Id);
        Assert.Equal(2, stored.RequiredRoleNames.Length);
        Assert.Equal(new[] { "users.view" }, stored.RequiredPermissionKeys);
        Assert.True(stored.IsVisibleTo(Set("Reviewer"), Set("users.view")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BothProvidersGenerateAdditiveVisibilityMigration(bool postgres)
    {
        using DbContext context = postgres
            ? new IamPostgreSqlDBContext(new DbContextOptionsBuilder<IamPostgreSqlDBContext>().UseNpgsql("Host=localhost;Database=test;Username=test;Password=test").Options, TimeProvider.System)
            : new IamSqlServerDBContext(new DbContextOptionsBuilder<IamSqlServerDBContext>().UseSqlServer("Server=localhost;Database=test;Integrated Security=true;TrustServerCertificate=true").Options, TimeProvider.System);
        var migrations = context.GetService<IMigrationsAssembly>().Migrations;
        var target = migrations.Keys.Single(key => key.EndsWith("MenuVisibility", StringComparison.Ordinal));
        var previous = migrations.Keys.Order(StringComparer.Ordinal).Last(key => string.CompareOrdinal(key, target) < 0);
        var sql = context.GetService<IMigrator>().GenerateScript(previous, target);
        Assert.Contains("RequiredPermissionKeys", sql, StringComparison.Ordinal);
        Assert.Contains("RequiredRoleNames", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("DROP TABLE", sql, StringComparison.OrdinalIgnoreCase);
    }
    private sealed class SqliteIamDBContext(DbContextOptions<SqliteIamDBContext> options) : IamDBContext(options, TimeProvider.System, new TestTenant())
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            foreach (var entity in builder.Model.GetEntityTypes())
            {
                var token = entity.FindProperty("RowVersion");
                if (token is not null) token.ValueGenerated = ValueGenerated.Never;
            }
        }
    }
    private sealed class TestTenant : ICurrentTenantProvider
    {
        public Guid TenantId { get; } = Guid.Parse("018fd81d-2c94-7ad0-a4a3-f1edb9c10001");
    }
}
