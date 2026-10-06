using Microsoft.Extensions.Logging;

namespace ST.Api.Logging;

internal static partial class PaymentLogDefinitions
{
    [LoggerMessage(EventId = 4000, Level = LogLevel.Error, Message = "Error processing M-Pesa C2B confirmation: {Error}")]
    public static partial void LogMpesaC2bConfirmationError(ILogger logger, string error);

    [LoggerMessage(EventId = 4001, Level = LogLevel.Information, Message = "Payment Reconciliation Worker started.")]
    public static partial void LogPaymentReconciliationWorkerStarted(ILogger logger);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Error, Message = "An error occurred during payment reconciliation.")]
    public static partial void LogPaymentReconciliationWorkerError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 4003, Level = LogLevel.Information, Message = "Payment Reconciliation Worker stopping.")]
    public static partial void LogPaymentReconciliationWorkerStopping(ILogger logger);

    [LoggerMessage(EventId = 4004, Level = LogLevel.Information, Message = "Found {Count} pending payments for reconciliation sweep.")]
    public static partial void LogFoundPendingPayments(ILogger logger, int count);

    [LoggerMessage(EventId = 4005, Level = LogLevel.Warning, Message = "Payment {PaymentId} has no ProviderReference. Skipping reconciliation.")]
    public static partial void LogPaymentMissingProviderReference(ILogger logger, Guid paymentId);

    [LoggerMessage(EventId = 4006, Level = LogLevel.Warning, Message = "Unknown payment status {Status} from provider {Provider}")]
    public static partial void LogUnknownPaymentStatus(ILogger logger, string status, string provider);

    [LoggerMessage(EventId = 4007, Level = LogLevel.Information, Message = "Reconciling payment {PaymentId} to {Status}.")]
    public static partial void LogReconcilingPaymentStatus(ILogger logger, Guid paymentId, string status);

    [LoggerMessage(EventId = 4008, Level = LogLevel.Warning, Message = "Failed to reconcile payment {PaymentId} against provider {Provider}")]
    public static partial void LogPaymentReconciliationFailed(ILogger logger, Guid paymentId, string provider, Exception ex);
}
