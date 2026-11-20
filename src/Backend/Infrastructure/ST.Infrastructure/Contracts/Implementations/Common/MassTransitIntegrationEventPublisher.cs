using MassTransit;
using MassTransit.DependencyInjection;
using MassTransit.EntityFrameworkCoreIntegration;
using MassTransit.Middleware.Outbox;
using Microsoft.EntityFrameworkCore;
using ST.Application.Contracts.Interfaces.Common;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.IAM.DataContext;

namespace ST.Infrastructure.Contracts.Implementations.Common;

internal sealed class MassTransitIntegrationEventPublisher(
    IBus bus,
    IamDBContext iam,
    IBusOutboxNotification notification,
    Bind<IBus, IClientFactory> clientFactory,
    IServiceProvider services) : IIntegrationEventPublisher
{
    public async Task PublishAsync<TIntegrationEvent>(
        TIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TIntegrationEvent : IIntegrationEvent
    {
        if (iam.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("IAM integration events require an active IAM transaction.");
        }

        // Keep each staged event independent of an earlier transaction attempt in this DI scope.
        using var outbox = new EntityFrameworkScopedBusContext<IBus, IamDBContext>(
            bus, iam, notification, clientFactory.Value, services);

        await outbox.PublishEndpoint.Publish(integrationEvent, cancellationToken).ConfigureAwait(false);
    }
}
