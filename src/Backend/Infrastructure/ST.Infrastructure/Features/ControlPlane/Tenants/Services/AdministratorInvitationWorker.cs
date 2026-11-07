using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ST.Infrastructure.Features.ControlPlane.Tenants.Services;

internal sealed partial class AdministratorInvitationWorker(
    IServiceScopeFactory scopes, TimeProvider timeProvider, ILogger<AdministratorInvitationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3), timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await using var scope = scopes.CreateAsyncScope();
            try
            {
                var delivery = scope.ServiceProvider.GetRequiredService<AdministratorInvitationDelivery>();
                await delivery.DeliverAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                LogLeaseChanged(logger);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                LogSweepFailure(logger, exception.GetType().Name);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "An administrator invitation changed during delivery.")]
    private static partial void LogLeaseChanged(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The administrator invitation delivery sweep failed ({FailureType}).")]
    private static partial void LogSweepFailure(ILogger logger, string failureType);
}
