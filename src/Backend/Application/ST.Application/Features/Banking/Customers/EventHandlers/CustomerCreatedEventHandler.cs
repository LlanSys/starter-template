using ST.Application.Contracts.Interfaces.Common;
using ST.SharedKernel.Extensions;
using ST.Application.Features.Banking.Customers.IntegrationEvents;
using ST.Application.Features.HR.Employees.IntegrationEvents;
using ST.Application.Features.IAM.Users.IntegrationEvents;
using ST.Application.Utilities;
using ST.Domain.Features.Banking.Customers.Events;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.EmailTemplates.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.Banking.Customers.EventHandlers;

public class CustomerCreatedEventHandler(
    IIntegrationEventPublisher integrationEventPublisher,
    ILogger<CustomerCreatedEventHandler> _logger)
    : INotificationHandler<CustomerCreatedEvent>
{
    public async Task Handle(CustomerCreatedEvent evt, CancellationToken cancellationToken)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(evt, nameof(evt));

            var integrationEvent = new CustomerCreatedIntegrationEvent(
                evt.CustomerId,
                evt.Number,
                evt.Name,
                evt.Email,
                evt.Type.ToDisplayString()

            );

            await integrationEventPublisher.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);

            LogDefinitions.LogCustomerCreatedIntegrationPublished(_logger, evt.CustomerId);
        }
        catch (Exception ex)
        {
            LogDefinitions.LogCustomerCreatedDomainEventHandlerError(_logger, evt?.Email ?? string.Empty, ex);
            throw;
        }
    }
}
