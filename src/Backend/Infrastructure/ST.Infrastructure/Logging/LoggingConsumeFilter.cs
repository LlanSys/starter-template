using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Infrastructure.Logging;

public class LoggingConsumeFilter<T>(ILogger<LoggingConsumeFilter<T>> logger, TimeProvider timeProvider) : IFilter<ConsumeContext<T>> where T : class
{
    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        ArgumentNullException.ThrowIfNull(context);

        var messageType = typeof(T).Name;
        var messageId = context.MessageId?.ToString() ?? "unknown";
        var retryCount = 0;
        if (context.Headers.TryGetHeader("MT-Redelivery-Count", out var retryCountValue) &&
            retryCountValue is not null)
        {
            _ = int.TryParse(retryCountValue.ToString(), out retryCount);
        }

        var isRedelivered = false;
        if (context.Headers.TryGetHeader("MT-Redelivered", out var isRedeliveredValue) &&
            isRedeliveredValue is not null)
        {
            _ = bool.TryParse(isRedeliveredValue.ToString(), out isRedelivered);
        }

        var startedAt = timeProvider.GetTimestamp();

        try
        {
            ArgumentNullException.ThrowIfNull(next);

            await next.Send(context).ConfigureAwait(false);

        }
        catch (Exception ex)
        {
            var elapsedMs = (long)timeProvider.GetElapsedTime(startedAt).TotalMilliseconds;

            MessageBusLogDefinitions.LogConsumeFailure(
                logger,
                messageType,
                messageId,
                elapsedMs,
                retryCount + 1,
                isRedelivered,
                ex.Message,
                ex);

            throw;
        }
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("loggingFilter");
    }
}