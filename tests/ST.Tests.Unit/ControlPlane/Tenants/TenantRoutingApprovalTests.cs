using ST.Application.Contracts.Interfaces.Common;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Shared.Exceptions;
using ST.Infrastructure.Contracts.Implementations.Common;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace ST.Tests.Unit.ControlPlane.Tenants;

public sealed class TenantRoutingApprovalTests
{
    [Theory]
    [InlineData(TenantStatus.PendingApproval)]
    [InlineData(TenantStatus.PendingProvisioning)]
    [InlineData(TenantStatus.Provisioning)]
    [InlineData(TenantStatus.ProvisioningFailed)]
    [InlineData(TenantStatus.Suspended)]
    public async Task NonActiveTenantCannotResolveDatabase(TenantStatus status)
    {
        var tenant = CreateTenant(status);
        var uow = Substitute.For<IControlPlaneUnitOfWork>();
        uow.Tenants.FindByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        var encryption = Substitute.For<IEncryptionService>();
        using var provider = CreateServices(tenant.Id, uow, encryption);
        var resolver = provider.GetRequiredService<ITenantConnectionResolver>();
        await Assert.ThrowsAsync<TenantNotResolvedException>(() => resolver.GetConnectionStringAsync());
        encryption.DidNotReceiveWithAnyArgs().Decrypt(default!);
    }

    [Fact]
    public async Task SuspensionBlocksPreviouslyCachedDatabaseRoute()
    {
        var tenant = CreateTenant(TenantStatus.Active);
        var uow = Substitute.For<IControlPlaneUnitOfWork>();
        uow.Tenants.FindByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        var encryption = Substitute.For<IEncryptionService>();
        encryption.Decrypt("encrypted-database").Returns("test-connection");
        using var provider = CreateServices(tenant.Id, uow, encryption);
        var resolver = provider.GetRequiredService<ITenantConnectionResolver>();
        Assert.Equal("test-connection", await resolver.GetConnectionStringAsync());
        tenant.Suspend();
        await Assert.ThrowsAsync<TenantNotResolvedException>(() => resolver.GetConnectionStringAsync());
        await Assert.ThrowsAsync<TenantNotResolvedException>(() => resolver.GetDatabaseProviderAsync());
        encryption.Received(1).Decrypt("encrypted-database");
    }

    private static ServiceProvider CreateServices(Guid tenantId, IControlPlaneUnitOfWork uow, IEncryptionService encryption)
    {
        var currentTenant = Substitute.For<ICurrentTenantProvider>();
        currentTenant.TenantId.Returns(tenantId);
        return new ServiceCollection().AddMemoryCache()
            .AddSingleton(uow).AddSingleton(encryption).AddSingleton(currentTenant)
            .AddScoped<ITenantConnectionResolver, TenantConnectionResolver>().BuildServiceProvider();
    }

    private static Tenant CreateTenant(TenantStatus status) => new()
    {
        Id = Guid.CreateVersion7(),
        Identifier = "test",
        DisplayName = "Test tenant",
        HostName = "test.local",
        CreatedBy = "creator-id",
        Status = status,
        DatabaseConnectionString = "encrypted-database",
        DatabaseProvider = "SqlServer"
    };
}
