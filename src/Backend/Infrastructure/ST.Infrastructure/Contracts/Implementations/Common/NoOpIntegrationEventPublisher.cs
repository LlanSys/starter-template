using ST.Application.Contracts.Interfaces.Common;
using ST.Domain.Shared.Contracts.Common;

namespace ST.Infrastructure.Contracts.Implementations.Common;

internal sealed class NoOpIntegrationEventPublisher : IIntegrationEventPublisher
{
    public Task PublishAsync<TIntegrationEvent>(
        TIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TIntegrationEvent : IIntegrationEvent
        => Task.CompletedTask;
}
