using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ST.Application.Features.Shared.WorkTasks;
using ST.SharedKernel.Features.Shared.Dashboard;

namespace ST.Infrastructure.Features.Shared.WorkTasks;

internal sealed partial class WorkTaskReconciliationWorker(IServiceScopeFactory scopes, TimeProvider clock,
    ILogger<WorkTaskReconciliationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cursors = new Dictionary<string, Guid?>();
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15), clock);
        using var stopTimer = stoppingToken.Register(timer.Dispose);

        while (await timer.WaitForNextTickAsync().ConfigureAwait(false) && !stoppingToken.IsCancellationRequested)
        {
            foreach (var kind in DashboardTaskCatalog.Definitions.Keys)
            {
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var service = scope.ServiceProvider.GetRequiredService<IWorkTaskService>();
                    cursors[kind] = await service.ReconcileAsync(kind, cursors.GetValueOrDefault(kind), stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception exception) { LogRetry(logger, exception); }
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Task completion history reconciliation will be retried.")]
    private static partial void LogRetry(ILogger logger, Exception exception);
}
