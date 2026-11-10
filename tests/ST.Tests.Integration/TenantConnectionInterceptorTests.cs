using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Common.Interceptors;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ST.Tests.Integration;

public class TenantConnectionInterceptorTests
{
    private class FakeDbConnection : DbConnection
    {
        private string _connectionString = "Server=OldServer;Database=SharedDB;";
        public override string ConnectionString { get => _connectionString; set => _connectionString = value ?? string.Empty; }
        public override string Database => "Database";
        public override string DataSource => "DataSource";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => ConnectionState.Closed;
        public override void ChangeDatabase(string databaseName) {}
        public override void Close() {}
        public override void Open() {}
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => null!;
        protected override DbCommand CreateDbCommand() => null!;
    }

    private class FakeTenantConnectionResolver : ITenantConnectionResolver
    {
        private readonly string _connectionString;
        public FakeTenantConnectionResolver(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Task<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<string?>(_connectionString);
        }

        public Task<string?> GetDatabaseProviderAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<string?>("SqlServer");
        }
    }

    [Fact]
    public async Task ConnectionOpeningAsync_ShouldChangeConnectionString_WhenResolverProvidesOne()
    {
        // Arrange
        var fakeResolver = new FakeTenantConnectionResolver("Server=NewServer;Database=TenantDB;");

        var services = new ServiceCollection();
        services.AddScoped<ITenantConnectionResolver>(_ => fakeResolver);
        var serviceProvider = services.BuildServiceProvider();

        var interceptor = new TenantConnectionInterceptor(serviceProvider);

        using var fakeConnection = new FakeDbConnection();

        var eventData = default(ConnectionEventData);

        // Act
        await interceptor.ConnectionOpeningAsync(fakeConnection, eventData, new InterceptionResult(), CancellationToken.None);

        // Assert
        Assert.Equal("Server=NewServer;Database=TenantDB;", fakeConnection.ConnectionString);
    }

    [Fact]
    public async Task ConnectionOpeningAsync_ShouldNotChangeConnectionString_WhenResolverProvidesNullOrEmpty()
    {
        // Arrange
        var fakeResolver = new FakeTenantConnectionResolver(string.Empty);

        var services = new ServiceCollection();
        services.AddScoped<ITenantConnectionResolver>(_ => fakeResolver);
        var serviceProvider = services.BuildServiceProvider();

        var interceptor = new TenantConnectionInterceptor(serviceProvider);

        using var fakeConnection = new FakeDbConnection();

        var eventData = default(ConnectionEventData);

        // Act
        await interceptor.ConnectionOpeningAsync(fakeConnection, eventData, new InterceptionResult(), CancellationToken.None);

        // Assert
        Assert.Equal("Server=OldServer;Database=SharedDB;", fakeConnection.ConnectionString);
    }

    [Fact]
    public async Task ReopeningForPooledRoutingRestoresTheOriginalConnection()
    {
        var resolver = Substitute.For<ITenantConnectionResolver>();
        resolver.GetConnectionStringAsync(Arg.Any<CancellationToken>()).Returns(
            Task.FromResult<string?>("Server=NewServer;Database=TenantDB;"), Task.FromResult<string?>(null));
        using var provider = new ServiceCollection().AddSingleton(resolver).BuildServiceProvider();
        var interceptor = new TenantConnectionInterceptor(provider);
        using var connection = new FakeDbConnection();

        await interceptor.ConnectionOpeningAsync(connection, default!, new InterceptionResult(), CancellationToken.None);
        Assert.Equal("Server=NewServer;Database=TenantDB;", connection.ConnectionString);
        await interceptor.ConnectionOpeningAsync(connection, default!, new InterceptionResult(), CancellationToken.None);
        Assert.Equal("Server=OldServer;Database=SharedDB;", connection.ConnectionString);
    }

    [Fact]
    public void ConnectionOpening_ShouldThrowNotSupportedException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var interceptor = new TenantConnectionInterceptor(serviceProvider);

        using var fakeConnection = new FakeDbConnection();

        var eventData = default(ConnectionEventData);

        // Act & Assert
        var ex = Assert.Throws<NotSupportedException>(() => interceptor.ConnectionOpening(fakeConnection, eventData, new InterceptionResult()));
        Assert.Contains("Synchronous connection opening is not supported", ex.Message);
    }
}
