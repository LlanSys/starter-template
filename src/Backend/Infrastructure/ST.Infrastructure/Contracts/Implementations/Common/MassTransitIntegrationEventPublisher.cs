using ST.Application.Contracts.Interfaces.Common;
using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using MassTransit;

namespace ST.Infrastructure.Contracts.Implementations.Common;

internal sealed class MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint) : IIntegrationEventPublisher
{
    public Task PublishAsync<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        where TIntegrationEvent : IIntegrationEvent
        => publishEndpoint.Publish(integrationEvent, cancellationToken);
}
