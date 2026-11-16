using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ST.Application.Features.ControlPlane.Kyc.Contracts;
using ST.Infrastructure.Configuration;

namespace ST.Infrastructure.Features.ControlPlane.Kyc;

internal sealed partial class KycDocumentSafetyWorker(IKycDocumentSafetyProcessor processor,
    IOptions<KycDocumentSafetySettings> options, TimeProvider clock,
    ILogger<KycDocumentSafetyWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.PollSeconds), clock);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try { await processor.ProcessBatchAsync(stoppingToken).ConfigureAwait(false); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception exception) { LogFailure(logger, exception); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal host shutdown leaves durable leases recoverable by the next worker.
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "KYC safety batch failed; pending evidence remains quarantined.")]
    private static partial void LogFailure(ILogger logger, Exception exception);
}
