using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ST.Persistence.Features.Shared.DataContext;
using System.Collections.Concurrent;

namespace ST.Tests.Integration;

public sealed class RabbitMqOutboxTransportTests
{
    [RabbitMqFact]
    [Trait("Category", "ExternalRabbitMq")]
    public async Task Ef_outbox_should_deliver_message_to_real_rabbitmq_consumer()
    {
        var hostName = GetRequiredEnvironmentVariable("BT_RABBITMQ_HOST");
        var virtualHost = GetRequiredEnvironmentVariable("BT_RABBITMQ_VIRTUAL_HOST");
        var username = GetRequiredEnvironmentVariable("BT_RABBITMQ_USERNAME");
        var password = GetRequiredEnvironmentVariable("BT_RABBITMQ_PASSWORD");
        var queueName = $"bt-outbox-certification-{Guid.CreateVersion7():N}";
        var databasePath = Path.Combine(Path.GetTempPath(), $"bt-outbox-{Guid.CreateVersion7():N}.db");
        var connectionString = $"Data Source={databasePath};Pooling=False";
        var messageId = Guid.CreateVersion7();

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<ST.Domain.Shared.Contracts.Common.ICurrentTenantProvider, TestTenantProvider>();
        builder.Services.AddSingleton<ST.Domain.Shared.Contracts.Common.ICurrentActorProvider, TestActorProvider>();
        builder.Services.AddSingleton<RabbitMqDeliveryProbe>();
        builder.Services.AddDbContext<SharedDBContext>(options => options.UseSqlite(connectionString));
        builder.Services.AddMassTransit(configurator =>
        {
            configurator.AddConsumer<RabbitMqCertificationConsumer>();
            configurator.AddEntityFrameworkOutbox<SharedDBContext>(outbox =>
            {
                outbox.UseSqlite();
                outbox.UseBusOutbox();
                outbox.QueryDelay = TimeSpan.FromMilliseconds(100);
            });

            configurator.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(hostName, virtualHost, host =>
                {
                    host.Username(username);
                    host.Password(password);
                });
                rabbit.ReceiveEndpoint(queueName, endpoint =>
                {
                    endpoint.AutoDelete = true;
                    endpoint.Durable = false;
                    endpoint.ConfigureConsumer<RabbitMqCertificationConsumer>(context);
                });
            });
        });

        var host = builder.Build();

        try
        {
            await host.StartAsync();

            await using var scope = host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<SharedDBContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            var probe = scope.ServiceProvider.GetRequiredService<RabbitMqDeliveryProbe>();
            var delivery = probe.ExpectAsync(messageId);

            await db.Database.EnsureCreatedAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();

            await publisher.Publish(new RabbitMqCertificationEvent(messageId, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            var delivered = await delivery.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(messageId, delivered.MessageId);

            var outboxDrained = await WaitForOutboxToDrainAsync(db, TimeSpan.FromSeconds(10));
            Assert.True(outboxDrained, "The consumer received the event, but the EF outbox did not drain.");
        }
        finally
        {
            await host.StopAsync();
            host.Dispose();
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    private static string GetRequiredEnvironmentVariable(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Required environment variable '{name}' is missing.");

    private static async Task<bool> WaitForOutboxToDrainAsync(SharedDBContext db, TimeSpan timeout)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < expiresAt)
        {
            if (!await db.Set<OutboxMessage>().AnyAsync())
            {
                return true;
            }

            await Task.Delay(100);
        }

        return false;
    }
}

internal sealed record RabbitMqCertificationEvent(Guid MessageId, DateTimeOffset OccurredAt);

internal sealed class RabbitMqCertificationConsumer(RabbitMqDeliveryProbe probe)
    : IConsumer<RabbitMqCertificationEvent>
{
    public Task Consume(ConsumeContext<RabbitMqCertificationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        probe.Delivered(context.Message);
        return Task.CompletedTask;
    }
}

internal sealed class RabbitMqDeliveryProbe
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<RabbitMqCertificationEvent>> _deliveries = new();

    public Task<RabbitMqCertificationEvent> ExpectAsync(Guid messageId)
    {
        var completion = new TaskCompletionSource<RabbitMqCertificationEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_deliveries.TryAdd(messageId, completion))
        {
            throw new InvalidOperationException($"A delivery expectation already exists for '{messageId}'.");
        }

        return completion.Task;
    }

    public void Delivered(RabbitMqCertificationEvent message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (_deliveries.TryRemove(message.MessageId, out var completion))
        {
            completion.TrySetResult(message);
        }
    }
}

internal sealed class RabbitMqFactAttribute : FactAttribute
{
    public RabbitMqFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("BT_RUN_RABBITMQ_TESTS"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Run scripts/test-local-messaging.ps1 to execute the real RabbitMQ transport certification.";
        }
    }
}

internal sealed class TestTenantProvider : ST.Domain.Shared.Contracts.Common.ICurrentTenantProvider
{
    public Guid TenantId => Guid.Empty;
}

internal sealed class TestActorProvider : ST.Domain.Shared.Contracts.Common.ICurrentActorProvider
{
    public string ActorId => "System";
}
