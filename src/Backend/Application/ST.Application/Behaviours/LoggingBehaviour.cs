using ST.Application.Utilities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Behaviours;

public class LoggingBehaviour<TRequest, TResponse>(ILogger<LoggingBehaviour<TRequest, TResponse>> logger, TimeProvider timeProvider)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var requestName = typeof(TRequest).Name;

        RequestLogDefinitions.LogRequestStarted(logger, requestName);

        var startedAt = timeProvider.GetTimestamp();
        var response = await next(cancellationToken).ConfigureAwait(false);
        var elapsedMs = (long)timeProvider.GetElapsedTime(startedAt).TotalMilliseconds;

        if (elapsedMs > 500)
        {
            RequestLogDefinitions.LogSlowRequest(logger, requestName, elapsedMs);
        }
        else
        {
            RequestLogDefinitions.LogRequestCompleted(logger, requestName, elapsedMs);
        }

        return response;
    }
}
