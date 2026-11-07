using ST.Application.Contracts.Interfaces.Common;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Contracts.Repositories;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Contracts.Implementations.Common;
using ST.Persistence.Common.Interceptors;
using ST.Persistence.Features.ControlPlane;
using ST.Persistence.Features.ControlPlane.DataContext;
using ST.Persistence.Features.ControlPlane.Tenants.Repositories;
using ST.Persistence.Features.Shared.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ST.Domain.Features.ControlPlane.Auditing.Contracts.Repositories;

namespace ST.Tests.Integration;

public class TenantConnectionResolverIntegrationTests
{
    private class TestTenantProvider : ICurrentTenantProvider
    {
        public Guid TenantId { get; set; }
    }

    private class TestActorProvider : ICurrentActorProvider
    {
        public string ActorId { get; set; } = "test-actor";
    }

    private class FakeEncryptionService : IEncryptionService
    {
        public string Encrypt(string plainText) => "ENCRYPTED:" + plainText;
        public string Decrypt(string cipherText) => cipherText.Replace("ENCRYPTED:", "");
        public string HashCode(string rawString) => "HASHED";
        public bool VerifyCode(string hashString, string rawString) => true;
    }

    [Fact]
    public async Task RealResolver_SuccessfullyFetchesDecryptsAndSwapsConnection()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var stampId = Guid.NewGuid();
        var distinctDatabaseName = "real_resolver_db_test";
        var distinctConnectionString = $"Server=TestServer;Database={distinctDatabaseName};";

        var services = new ServiceCollection();

        var dbName = "ControlPlaneDB_" + Guid.NewGuid();
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<ControlPlaneDBContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddScoped<ICurrentActorProvider, TestActorProvider>();

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IDeploymentStampRepository, DeploymentStampRepository>();
        // Add fake impersonation repo just to satisfy UnitOfWork
        var impersonationMock = NSubstitute.Substitute.For<IImpersonationRecordRepository>();
        services.AddScoped(_ => impersonationMock);

        services.AddScoped<IControlPlaneUnitOfWork, ControlPlaneUnitOfWork>();

        // 2. Setup the dependencies for the real resolver
        services.AddMemoryCache();
        var tenantProviderMock = new TestTenantProvider { TenantId = tenantId };
        services.AddScoped<ICurrentTenantProvider>(_ => tenantProviderMock);

        var encryptionMock = new FakeEncryptionService();
        services.AddScoped<IEncryptionService>(_ => encryptionMock);

        // 3. Register the REAL resolver
        services.AddScoped<ITenantConnectionResolver, TenantConnectionResolver>();

        var serviceProvider = services.BuildServiceProvider();

        // 4. Seed the ControlPlane database
        using (var scope = serviceProvider.CreateScope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IControlPlaneUnitOfWork>();

            var stamp = new DeploymentStamp
            {
                Id = stampId,
                Name = "Test Stamp",
                IsolationTier = IsolationTier.Isolated,
                CreatedBy = "System",
                DatabaseConnectionString = encryptionMock.Encrypt(distinctConnectionString), // Encrypted!
                TargetResourceGroup = "TestRG"
            };
            await uow.DeploymentStamps.CreateAsync(stamp);

            var tenant = new Tenant
            {
                Id = tenantId,
                DeploymentStampId = stampId,
                Identifier = "testtenant",
                DisplayName = "Test Tenant",
                HostName = "test.local",
                Status = TenantStatus.Active,
                CreatedBy = "System",
                SubscriptionTier = SubscriptionTier.Enterprise
            };
            await uow.Tenants.CreateAsync(tenant);
            await uow.CompleteAsync();
        }

        var resolver = serviceProvider.GetRequiredService<ITenantConnectionResolver>();
        var resolvedString = await resolver.GetConnectionStringAsync(CancellationToken.None);
        Assert.NotNull(resolvedString);
        Assert.Contains(distinctDatabaseName, resolvedString);

        // 5. Build a target DbContext (SharedDBContext) using the interceptor
        var builder = new DbContextOptionsBuilder<SharedDBContext>();
        // A placeholder connection string to prove it gets swapped
        builder.UseSqlServer("Server=OldServer;Database=SharedDB;");
        builder.AddInterceptors(new TenantConnectionInterceptor(serviceProvider));

        using var context = new SharedDBContext(builder.Options, TimeProvider.System, tenantProviderMock, new TestActorProvider());

        // This will trigger the interceptor, which calls the resolver, which hits the cache + swap
        var connection = context.Database.GetDbConnection();
        var ex = await Record.ExceptionAsync(async () => await context.Database.OpenConnectionAsync());

        // Assert
        Assert.NotNull(ex); // Fails to connect because it's a dummy connection string, but...

        // The connection string MUST have been updated to the decrypted value from the ControlPlane DB!
        Assert.Contains(distinctDatabaseName, connection.ConnectionString);
    }
}
