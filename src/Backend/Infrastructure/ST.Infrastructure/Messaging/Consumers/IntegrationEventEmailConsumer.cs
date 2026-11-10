using ST.SharedKernel.Features.Shared.Notifications.Dtos;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.SharedKernel.Extensions;
using ST.Application.Features.IAM.Users.IntegrationEvents;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Application.Features.Shared.EmailTemplates.Mappings;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Shared.Entities;
using ST.Domain.Features.Shared.FailedMessages.Enums;

using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.EmailTemplates.Enums;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ST.Infrastructure.Messaging.Consumers;

public abstract class IntegrationEventEmailConsumer<TEvent>(
    IEmailComposer<TEvent> composer,
    ISharedUnitOfWork sharedUnitOfWork,
    ILogger<IntegrationEventEmailConsumer<TEvent>> logger, TimeProvider timeProvider) : IConsumer<TEvent> where TEvent : class, IIntegrationEvent

{
    public async Task Consume(ConsumeContext<TEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        var evt = context.Message;
        var retryCount = context.GetRetryCount();
        var messageId = context.MessageId?.ToString() ?? Guid.CreateVersion7().ToString();

        try
        {
            var composed = await composer.ComposeAsync(evt, context.CancellationToken).ConfigureAwait(false);
            if (!composed.IsSuccess || composed.Data is null)
            {
                MessageBusLogDefinitions.LogEmailCompositionFailed(logger, messageId);
                return;
            }

            await context.Publish(new SendEmailRequest
            {
                To = composed.Data.RecipientEmail,
                Subject = composed.Data.Subject,
                Body = composed.Data.Body

            }, context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (retryCount >= 4)
        {
            MessageBusLogDefinitions.LogPermanentEventFailure(logger, typeof(TEvent).Name, retryCount + 1, ex);

            var failed = FailedMessage.RecordPermanentFailure(
                timeProvider.GetUtcNow(),
                messageId,
                typeof(TEvent).FullName!,
                JsonSerializer.Serialize(evt),
                ex.Message,
                retryCount + 1,
                GetType().Name,
                messageId,
                ex.StackTrace);

            await sharedUnitOfWork.FailedMessageRepository.CreateAsync(failed, context.CancellationToken).ConfigureAwait(false);
            await sharedUnitOfWork.CompleteAsync(context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            MessageBusLogDefinitions.LogTransientEventFailure(logger, typeof(TEvent).Name, retryCount + 1, ex);
            throw; // let MassTransit retry
        }
    }
}
