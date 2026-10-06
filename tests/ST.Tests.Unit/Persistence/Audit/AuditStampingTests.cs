using ST.Domain.Features.HR.Departments.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.HR.DataContext;
using Microsoft.EntityFrameworkCore;

namespace ST.Tests.Unit.Persistence.Audit;

public sealed class AuditStampingTests
{
    private static readonly Guid TenantId = Guid.Parse("0194f700-0000-7000-8000-000000000001");

    [Fact]
    public async Task SaveChangesAsync_Should_Stamp_Current_Actor_And_Tenant_On_Create()
    {
        await using var context = CreateContext("create-audit", "actor-1");

        var department = Department.Create("qa", "Quality Assurance", "Test department", "initial");
        context.Departments.Add(department);

        await context.SaveChangesAsync();

        Assert.Equal("actor-1", department.CreatedBy);
        Assert.Equal(TenantId, department.TenantId);
        Assert.Null(department.UpdatedBy);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Stamp_Current_Actor_On_Update()
    {
        var databaseName = $"update-audit-{Guid.CreateVersion7()}";
        await using var createContext = CreateContext(databaseName, "creator");

        var department = Department.Create("ops", "Operations", "Operations", "initial");
        createContext.Departments.Add(department);
        await createContext.SaveChangesAsync();

        await using var updateContext = CreateContext(databaseName, "updater");
        var savedDepartment = await updateContext.Departments.SingleAsync(department => department.Code == "OPS");
        savedDepartment.Update("ops", "Operations", "Updated", true, "domain");

        await updateContext.SaveChangesAsync();

        Assert.Equal("updater", savedDepartment.UpdatedBy);
        Assert.NotNull(savedDepartment.UpdatedAt);
    }

    private static HrDBContext CreateContext(string databaseName, string actorId)
    {
        var options = new DbContextOptionsBuilder<HrDBContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new HrDBContext(
            options,
            new FixedTenantProvider(TenantId),
            new FixedActorProvider(actorId));
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
