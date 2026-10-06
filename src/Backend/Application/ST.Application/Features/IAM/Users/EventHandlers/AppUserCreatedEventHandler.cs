using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Users.IntegrationEvents;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Users.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Features.IAM.Users.EventHandlers;

internal sealed class AppUserCreatedEventHandler(IIntegrationEventPublisher integrationEventPublisher, ILogger<AppUserCreatedEventHandler> logger)
    : INotificationHandler<AppUserCreatedEvent>
{
    public async Task Handle(AppUserCreatedEvent evt, CancellationToken cancellationToken)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(evt);

            var integrationEvent = new AppUserCreatedIntegrationEvent(
                evt.UserId,
                evt.TenantId,
                evt.UserName,
                evt.FullName,
                evt.Email);

            await integrationEventPublisher.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);

            LogDefinitions.LogAppUserCreatedIntegrationPublished(logger, evt.UserId);

        }
        catch (Exception ex)
        {
            LogDefinitions.LogAppUserCreatedIntegrationPublishFailed(logger, evt?.UserId ?? string.Empty, ex);
            throw;
        }
    }
}
