using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using System;
using System.Text.Json;

namespace ST.Infrastructure.Jobs;

// DisallowConcurrentExecution prevents the same specific job from running twice at once
[DisallowConcurrentExecution]
public class MediatorSerializedJob(IServiceProvider _serviceProvider) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        // Get the serialized data from the job map
        var data = context.MergedJobDataMap.GetString("CommandData");
        var typeName = context.MergedJobDataMap.GetString("CommandType");

        if (string.IsNullOrWhiteSpace(data) || string.IsNullOrWhiteSpace(typeName)) return;

        // Reconstruct the Type
        var type = Type.GetType(typeName);
        if (type == null)
        {
            return;
        }

        // Deserialize back to the original Request object
        if (JsonSerializer.Deserialize(data, type) is IRequest request)
        {
            // Explicitly creating a scope ensures that the UnitOfWork and DBContext
            // used by the handler are fresh and disposed correctly.
            await using var scope = _serviceProvider.CreateAsyncScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            // Send it back through MediatR (This triggers EmailHandler, etc.)
            await sender.Send(request, cancellationToken).ConfigureAwait(false);
        }
    }

}
