using ST.Domain.Features.IAM.Users.Events;
using ST.Persistence.Features.Shared.DataContext;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using MassTransit.Testing;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Serilog;
using System.Globalization;
using ST.Domain.Shared.Contracts.Common;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ST.Tests.Integration;

public class OutboxIntegrationTests
{
    private sealed class DummyTenantProvider : ICurrentTenantProvider
    {
        public Guid TenantId => Guid.Empty;
    }
    private sealed class DummyActorProvider : ICurrentActorProvider
    {
        public string ActorId => "TestActor";
    }
    [Fact]
    public async Task Should_Persist_Event_To_Outbox()
    {
        // Arrange
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var provider = new ServiceCollection()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<ICurrentTenantProvider, DummyTenantProvider>()
            .AddSingleton<ICurrentActorProvider, DummyActorProvider>()
            .AddDbContext<SharedDBContext>(o => o.UseSqlite(connection))
            .AddMassTransitTestHarness(x =>
            {
                x.AddEntityFrameworkOutbox<SharedDBContext>(o =>
                {
                    o.UseSqlite();
                    o.UseBusOutbox();
                });

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SharedDBContext>();

        await db.Database.EnsureCreatedAsync();

        var message = new AppUserCreatedEvent(Guid.CreateVersion7().ToString(), Guid.CreateVersion7(), "user", "Test User", "user@example.test", DateTimeOffset.UnixEpoch);

        // Act
        await using var tx = await db.Database.BeginTransactionAsync();

        var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        await publisher.Publish(message);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        // Assert
        var outboxMessages = await db.Set<OutboxMessage>().ToListAsync();
        Assert.Single(outboxMessages);
    }

    [Fact]
    public async Task Outbox_Should_Capture_All_Events()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var provider = new ServiceCollection()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<ICurrentTenantProvider, DummyTenantProvider>()
            .AddSingleton<ICurrentActorProvider, DummyActorProvider>()
            .AddDbContext<SharedDBContext>(options =>
                options.UseSqlite(connection))
            .AddMassTransit(x =>
            {
                x.AddEntityFrameworkOutbox<SharedDBContext>(o =>
                {
                    o.UseSqlite();
                    o.UseBusOutbox();
                });

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);

        await using var scope = provider.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<SharedDBContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        await db.Database.EnsureCreatedAsync();

        var clientEvent = new AppUserCreatedEvent(Guid.CreateVersion7().ToString(), Guid.CreateVersion7(), "user", "Test User", "test@example.test", DateTimeOffset.UnixEpoch);

        await using var tx = await db.Database.BeginTransactionAsync();

        await publisher.Publish(clientEvent);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        var existsInOutbox = await db.Set<OutboxMessage>()
            .AnyAsync(m => m.MessageType.Contains(nameof(AppUserCreatedEvent)));

        Assert.True(existsInOutbox);
    }

}
