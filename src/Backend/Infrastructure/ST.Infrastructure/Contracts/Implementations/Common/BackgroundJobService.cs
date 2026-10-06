using ST.Application.Contracts.Interfaces.Common;
using ST.Infrastructure.Jobs;
using ST.Infrastructure.Logging;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quartz;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using TriggerBuilder = Quartz.TriggerBuilder;

namespace ST.Infrastructure.Contracts.Implementations.Common;

internal sealed class BackgroundJobService(ISchedulerFactory _schedulerFactory, ILogger<BackgroundJobService> logger) : IBackgroundJobService
{
    public void Enqueue(IRequest request)
    {
        try
        {
            // Get the type for serialization/deserialization later
            var type = request.GetType();

            var job = JobBuilder.Create<MediatorSerializedJob>()
                .WithIdentity($"{type.Name}_{Guid.CreateVersion7()}", "MediatRGroup")
                .UsingJobData("CommandData", JsonSerializer.Serialize(request))
                .UsingJobData("CommandType", type.AssemblyQualifiedName!)
                .Build();

            var trigger = TriggerBuilder.Create()
                .StartNow()
                .Build();

            // Fire and forget: schedule it on the Quartz scheduler
            _ = Task.Run(async () =>
            {
                var scheduler = await _schedulerFactory.GetScheduler().ConfigureAwait(false);
                await scheduler.ScheduleJob(job, trigger).ConfigureAwait(false);
            });
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogBackgroundJobEnqueueError(logger, request.GetType().Name, ex);
            throw;
        }
    }

    public async Task EnqueueAsync(IRequest? request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var type = request.GetType();
        var scheduler = await _schedulerFactory.GetScheduler(ct).ConfigureAwait(false);

        // Define the Job
        var job = JobBuilder.Create<MediatorSerializedJob>()
            .WithIdentity($"{type.Name}_{Guid.CreateVersion7()}", "MediatRGroup")
            .UsingJobData("CommandData", JsonSerializer.Serialize(request))
            .UsingJobData("CommandType", type.AssemblyQualifiedName!)
            .Build();

        // Define the Trigger (Execute now)
        var trigger = TriggerBuilder.Create()
            .StartNow()
            .Build();

        // Schedule the job
        await scheduler.ScheduleJob(job, trigger, ct).ConfigureAwait(false);
    }


}
