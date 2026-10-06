using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ST.Infrastructure.Logging;

public class LoggingConsumeFilter<T>(ILogger<LoggingConsumeFilter<T>> logger) : IFilter<ConsumeContext<T>> where T : class
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

        var stopwatch = Stopwatch.StartNew();

        try
        {
            ArgumentNullException.ThrowIfNull(next);

            await next.Send(context).ConfigureAwait(false);

            stopwatch.Stop();

        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            MessageBusLogDefinitions.LogConsumeFailure(
                logger,
                messageType,
                messageId,
                stopwatch.ElapsedMilliseconds,
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