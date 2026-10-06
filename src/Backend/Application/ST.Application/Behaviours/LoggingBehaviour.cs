using ST.Application.Utilities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ST.Application.Behaviours;

public class LoggingBehaviour<TRequest, TResponse>(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var requestName = typeof(TRequest).Name;

        RequestLogDefinitions.LogRequestStarted(logger, requestName);

        var sw = Stopwatch.StartNew();
        var response = await next(cancellationToken).ConfigureAwait(false);
        sw.Stop();

        if (sw.ElapsedMilliseconds > 500)
        {
            RequestLogDefinitions.LogSlowRequest(logger, requestName, sw.ElapsedMilliseconds);
        }
        else
        {
            RequestLogDefinitions.LogRequestCompleted(logger, requestName, sw.ElapsedMilliseconds);
        }

        return response;
    }
}
