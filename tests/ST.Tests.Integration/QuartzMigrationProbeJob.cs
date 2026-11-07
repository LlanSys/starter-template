using Quartz;

namespace ST.Tests.Integration;

public sealed class QuartzMigrationProbeJob(TaskCompletionSource<string?> completed) : IJob
{
    public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        completed.TrySetResult(context.MergedJobDataMap.GetString("marker"));
        return ValueTask.CompletedTask;
    }
}
