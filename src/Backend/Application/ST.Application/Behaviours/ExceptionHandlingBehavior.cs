using ST.Application.Utilities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Behaviours;

public class ExceptionHandlingBehavior<TRequest, TResponse>(ILogger<TRequest> logger) : IPipelineBehavior<TRequest, TResponse> where TRequest
    : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        try
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, typeof(TRequest).Name, ex);
            throw;
        }
    }

}
