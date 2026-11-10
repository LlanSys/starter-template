using ST.Domain.Features.IAM.Menus.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.IAM.DataContext;
using Microsoft.EntityFrameworkCore;

namespace ST.Tests.Unit.Persistence.Audit;

public sealed class AuditStampingTests
{
    private static readonly Guid TenantId = Guid.Parse("0194f700-0000-7000-8000-000000000001");
    private static readonly DateTimeOffset CreatedAt = new(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = CreatedAt.AddMinutes(5);

    [Fact]
    public async Task SaveChangesAsync_Should_Stamp_Current_Actor_And_Tenant_On_Create()
    {
        await using var context = CreateContext("create-audit", "actor-1", CreatedAt);

        var menu = MenuItem.Create(null, "qa", "Quality Assurance", "Test menu", "/qa", "Menu", "Sidebar", null, null, 1, "initial");
        context.MenuItems.Add(menu);

        await context.SaveChangesAsync();

        Assert.Equal("actor-1", menu.CreatedBy);
        Assert.Equal(CreatedAt, menu.CreatedAt);
        Assert.Equal(TenantId, menu.TenantId);
        Assert.Null(menu.UpdatedBy);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Stamp_Current_Actor_On_Update()
    {
        var databaseName = $"update-audit-{Guid.CreateVersion7()}";
        await using var createContext = CreateContext(databaseName, "creator", CreatedAt);

        var menu = MenuItem.Create(null, "ops", "Operations", "Operations", "/ops", "Menu", "Sidebar", null, null, 1, "initial");
        createContext.MenuItems.Add(menu);
        await createContext.SaveChangesAsync();

        await using var updateContext = CreateContext(databaseName, "updater", UpdatedAt);
        var savedMenu = await updateContext.MenuItems.SingleAsync(menu => menu.Key == "ops");
        savedMenu.Update(UpdatedAt, null, "ops", "Operations", "Updated", "/ops", "Menu", "Sidebar", null, null, 1, true, "domain");

        await updateContext.SaveChangesAsync();

        Assert.Equal("updater", savedMenu.UpdatedBy);
        Assert.Equal(UpdatedAt, savedMenu.UpdatedAt);
    }

    private static IamDBContext CreateContext(string databaseName, string actorId, DateTimeOffset now)
    {
        var options = new DbContextOptionsBuilder<IamDBContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new IamDBContext(
            options,
            new FixedClock(now),
            new FixedTenantProvider(TenantId),
            new FixedActorProvider(actorId));
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FixedTenantProvider(Guid tenantId) : ICurrentTenantProvider
    {
        public Guid TenantId { get; } = tenantId;
    }

    private sealed class FixedActorProvider(string actorId) : ICurrentActorProvider
    {
        public string ActorId { get; } = actorId;
    }
}
