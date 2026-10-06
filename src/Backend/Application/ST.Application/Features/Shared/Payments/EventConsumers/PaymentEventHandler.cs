using ST.Domain.Features.Shared.Payments.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using ST.Application.Utilities;

namespace ST.Application.Features.Shared.Payments.EventConsumers;

public sealed class PaymentEventHandler(ILogger<PaymentEventHandler> logger) :
    INotificationHandler<PaymentCompletedEvent>,
    INotificationHandler<PaymentFailedEvent>,
    INotificationHandler<PaymentCancelledEvent>
{
    public Task Handle(PaymentCompletedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        LogDefinitions.LogPaymentCompleted(
            logger,
            notification.CustomerReference,
            notification.Amount.Amount,
            notification.Amount.Currency);

        // TODO: Update specific business domains (e.g. SACCO Deposits, Insurtech Policies)
        return Task.CompletedTask;
    }

    public Task Handle(PaymentFailedEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        LogDefinitions.LogPaymentFailed(
            logger,
            notification.CustomerReference,
            notification.FailureReason);

        // TODO: Handle failure logic (e.g. notify user, mark policy as inactive)
        return Task.CompletedTask;
    }

    public Task Handle(PaymentCancelledEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        LogDefinitions.LogPaymentCancelled(
            logger,
            notification.CustomerReference,
            notification.Reason);

        // TODO: Handle cancellation logic (e.g. notify user without marking account as strictly 'failed')
        return Task.CompletedTask;
    }
}
