using ST.Api.Logging;
using ST.Application.Features.Shared.Payments.Contracts.Interfaces;
using ST.Domain.Features.Shared.Contracts;
using ST.Domain.Features.Shared.Payments.Enums;
using ST.SharedKernel.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ST.Api.Features.Shared.Payments.Workers;

/// <summary>
/// A background service that periodically checks for pending payments
/// and queries the payment provider to reconcile their true status.
/// </summary>
public sealed class PaymentReconciliationWorker(
    IServiceProvider serviceProvider,
    TimeProvider timeProvider,
    ILogger<PaymentReconciliationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        PaymentLogDefinitions.LogPaymentReconciliationWorkerStarted(logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReconcilePendingPaymentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                PaymentLogDefinitions.LogPaymentReconciliationWorkerError(logger, ex);
            }

            // Wait 5 minutes before sweeping again
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }

        PaymentLogDefinitions.LogPaymentReconciliationWorkerStopping(logger);
    }

    private async Task ReconcilePendingPaymentsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var sharedUnitOfWork = scope.ServiceProvider.GetRequiredService<ISharedUnitOfWork>();
        var paymentGateway = scope.ServiceProvider.GetRequiredService<IPaymentGateway>();

        // Find payments that have been in Initiated status for more than 5 minutes
        var threshold = timeProvider.GetUtcNow().AddMinutes(-5);

        // Sweeping pending records
        var pendingRecords = await sharedUnitOfWork.PaymentRecordRepository.ListAsync(
            q => q.Where(p => p.Status == PaymentStatus.Initiated && p.CreatedAt <= threshold).Take(50),
            cancellationToken);

        if (pendingRecords.Count == 0)
        {
            return;
        }

        PaymentLogDefinitions.LogFoundPendingPayments(logger, pendingRecords.Count);

        foreach (var record in pendingRecords)
        {
            if (string.IsNullOrWhiteSpace(record.ProviderReference))
            {
                PaymentLogDefinitions.LogPaymentMissingProviderReference(logger, record.Id);
                continue;
            }

            try
            {
                var gatewayResponse = await paymentGateway.GetStatusAsync(record.ProviderReference, record.Provider, cancellationToken);

                if (gatewayResponse.IsSuccess && gatewayResponse.Data != null && !string.IsNullOrWhiteSpace(gatewayResponse.Data.Status))
                {
                    // Map provider status to standard enum safely
                    PaymentStatus parsedStatus;
                    if (Enum.TryParse<PaymentStatus>(gatewayResponse.Data.Status, true, out var parsed))
                    {
                        parsedStatus = parsed;
                    }
                    else if (gatewayResponse.Data.Status.Equals("unpaid", StringComparison.OrdinalIgnoreCase))
                    {
                        parsedStatus = PaymentStatus.Pending;
                    }
                    else if (gatewayResponse.Data.Status.Equals("paid", StringComparison.OrdinalIgnoreCase))
                    {
                        parsedStatus = PaymentStatus.Success;
                    }
                    else
                    {
                        PaymentLogDefinitions.LogUnknownPaymentStatus(logger, gatewayResponse.Data.Status, record.Provider);
                        continue;
                    }

                    if (parsedStatus == PaymentStatus.Success)
                    {
                        PaymentLogDefinitions.LogReconcilingPaymentStatus(logger, record.Id, "Success");
                        record.UpdateStatus(PaymentStatus.Success, statusMessage: "Reconciled from background gateway check");
                    }
                    else if (parsedStatus == PaymentStatus.Failed)
                    {
                        PaymentLogDefinitions.LogReconcilingPaymentStatus(logger, record.Id, "Failed");
                        record.UpdateStatus(PaymentStatus.Failed, statusMessage: "Reconciled from background gateway check");
                    }
                    else if (parsedStatus == PaymentStatus.Cancelled)
                    {
                        PaymentLogDefinitions.LogReconcilingPaymentStatus(logger, record.Id, "Cancelled");
                        record.UpdateStatus(PaymentStatus.Cancelled, statusMessage: "Reconciled from background gateway check");
                    }
                }
            }
            catch (Exception ex)
            {
                PaymentLogDefinitions.LogPaymentReconciliationFailed(logger, record.Id, record.Provider, ex);
            }
        }

        // Commit the transaction to save all state transitions and dispatch domain events
        await sharedUnitOfWork.CompleteAsync(cancellationToken);
    }
}
