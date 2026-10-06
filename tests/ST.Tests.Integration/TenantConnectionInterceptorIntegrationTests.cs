using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Common.Interceptors;
using ST.Persistence.Features.Shared.DataContext;
using ST.Tests.Integration.TestFixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ST.Tests.Integration;

public abstract class TenantConnectionInterceptorIntegrationTests<TFixture> : IClassFixture<TFixture> where TFixture : DbFixture
{
    private readonly DbContextOptions<SharedDBContext> _options;
    private readonly TFixture _fixture;

    protected TenantConnectionInterceptorIntegrationTests(TFixture fixture)
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

    [Fact]
    public async Task Interceptor_SuccessfullySwapsConnection_WhenTenantConnectionStringIsProvided()
    {
        // We will assert that the DbConnection is physically switched by verifying its Database name.
        var defaultConnectionString = _fixture.GetConnectionString();
        var defaultDatabaseName = GetDatabaseName(defaultConnectionString);

        var distinctConnectionString = defaultConnectionString.Replace(defaultDatabaseName, "isolated_db_test");
        var distinctDatabaseName = "isolated_db_test";

        var services = new ServiceCollection();

        // Mock a tenant resolver that returns a distinct connection string
        services.AddScoped<ITenantConnectionResolver>(sp => new FakeTenantConnectionResolver(Guid.NewGuid(), distinctConnectionString));

        var serviceProvider = services.BuildServiceProvider();

        // Register interceptor with the service provider
        var builder = new DbContextOptionsBuilder<SharedDBContext>(_options);
        builder.AddInterceptors(new TenantConnectionInterceptor(serviceProvider));

        using var context = new SharedDBContext(builder.Options, new TestTenantProvider(), new TestActorProvider());

        // Ensure the connection is actually opened so the interceptor fires
        var connection = context.Database.GetDbConnection();

        // We expect it to fail to open because isolated_db_test doesn't exist,
        // but if it fails with a database-doesn't-exist error, it PROVES the connection string was successfully swapped!
        var ex = await Record.ExceptionAsync(async () => await context.Database.OpenConnectionAsync());

        Assert.NotNull(ex);
        // The fact that it throws proves it attempted to connect to the new DB.
        // We also check the connection string directly:
        Assert.Contains(distinctDatabaseName, connection.ConnectionString);
    }

    private string GetDatabaseName(string connectionString)
    {
        var builder = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = connectionString };
        if (builder.TryGetValue("Database", out var db)) return db.ToString()!;
        if (builder.TryGetValue("Initial Catalog", out var catalog)) return catalog.ToString()!;
        return "";
    }

    private class FakeTenantConnectionResolver : ITenantConnectionResolver
    {
        public Guid? CurrentTenantId { get; }
        private readonly string? _connectionString;

        public FakeTenantConnectionResolver(Guid tenantId, string connectionString)
        {
            CurrentTenantId = tenantId;
            _connectionString = connectionString;
        }

        public Task<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_connectionString);

        public Task<string?> GetDatabaseProviderAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }

    private class TestTenantProvider : ICurrentTenantProvider
    {
        public Guid TenantId { get; set; }
    }

    private class TestActorProvider : ICurrentActorProvider
    {
        public string ActorId { get; set; } = "test-actor";
    }
}
