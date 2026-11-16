using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ST.Application.Features.ControlPlane.Kyc.Contracts;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Entities;

namespace ST.Infrastructure.Features.ControlPlane.Kyc;

internal sealed partial class KycRetentionWorker(IServiceScopeFactory scopes, TimeProvider clock,
    ILogger<KycRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1), clock);
        using var stopTimer = stoppingToken.Register(timer.Dispose);

        while (await timer.WaitForNextTickAsync().ConfigureAwait(false) && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                Guid[] ids;

                await using (var scope = scopes.CreateAsyncScope())
                {
                    var unit = scope.ServiceProvider.GetRequiredService<IControlPlaneUnitOfWork>();
                    var now = clock.GetUtcNow();
                    var cases = unit.KycRetentionCases.FindAll();
                    ids = await unit.Tenants.FindAll().Where(t => !t.IsDemo && t.Id != Tenant.PlatformTenantId
                        && (!cases.Any(c => c.TenantId == t.Id)
                            || cases.Any(c => c.TenantId == t.Id && !c.Held
                                && ((c.Phase == "Retained" && c.RetainUntil <= now)
                                    || ((c.Phase == "Recovery" || c.Phase == "Deleting") && c.RecoveryUntil <= now
                                        && (c.LeaseUntil == null || c.LeaseUntil <= now) && (c.NextAttemptAt == null || c.NextAttemptAt <= now))))))
                        .OrderBy(t => t.Id).Select(t => t.Id).Take(25).ToArrayAsync(stoppingToken).ConfigureAwait(false);
                }

                foreach (var id in ids)
                {
                    try
                    {
                        await using var scope = scopes.CreateAsyncScope();
                        await scope.ServiceProvider.GetRequiredService<IKycRetentionService>().ProcessAsync(id, stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                    catch (Exception exception) { LogFailure(logger, exception); }
                }
                await using var orphanScope = scopes.CreateAsyncScope();
                await orphanScope.ServiceProvider.GetRequiredService<IKycRetentionService>().ReconcileOrphansAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { LogFailure(logger, exception); }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "KYC retention processing will be retried.")]
    private static partial void LogFailure(ILogger logger, Exception exception);
}
