using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Enums;

namespace ST.Infrastructure.Features.ControlPlane.Provisioning;

internal sealed partial class ProvisioningTimeoutWorker(
    IServiceScopeFactory scopes,
    TimeProvider clock,
    ILogger<ProvisioningTimeoutWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30), clock);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var unit = scope.ServiceProvider.GetRequiredService<IControlPlaneUnitOfWork>();
                var now = clock.GetUtcNow();
                var expired = await unit.Tenants.FindAll().AsTracking()
                    .Where(tenant => tenant.Status == TenantStatus.Provisioning
                        && (tenant.ProvisioningOperationId == null || tenant.ProvisioningDeadline <= now))
                    .OrderBy(tenant => tenant.ProvisioningDeadline)
                    .Take(20).ToArrayAsync(stoppingToken).ConfigureAwait(false);
                foreach (var tenant in expired)
                {
                    tenant.MarkAsProvisioningFailed();
                }

                if (expired.Length > 0)
                {
                    await unit.CompleteAsync(stoppingToken).ConfigureAwait(false);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                LogChanged(logger);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                LogSweepFailed(logger, exception);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "A provisioning attempt completed during timeout processing.")]
    private static partial void LogChanged(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Provisioning timeout processing failed.")]
    private static partial void LogSweepFailed(ILogger logger, Exception exception);
}
