using ST.Application.Features.Shared.Payments.CommandHandlers.Mpesa;
using ST.Application.Features.Shared.Payments.Contracts.Interfaces;
using ST.Domain.Features.Shared.Contracts;
using ST.Domain.Features.Shared.Payments.Enums;
using ST.Domain.Features.Shared.Payments.Events;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Payments.Dtos;

using MediatR;

namespace ST.Application.Features.Shared.Payments.CommandHandlers;

internal sealed class ProcessPaymentWebhookHandler(
    IPaymentWebhookVerifier paymentWebhookVerifier,
    ISharedUnitOfWork sharedUnitOfWork, TimeProvider timeProvider)
    : IRequestHandler<ProcessPaymentWebhookCommand, AppResponse<PaymentWebhookVerificationResponse>> //ProcessMpesaStkCallbackCommand
{
    public async Task<AppResponse<PaymentWebhookVerificationResponse>> Handle(
        ProcessPaymentWebhookCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var verificationResult = await paymentWebhookVerifier.VerifyAsync(
            request.Provider,
            request.Payload,
            request.SignatureHeader,
            cancellationToken).ConfigureAwait(false);

        if (!verificationResult.IsSuccess || verificationResult.Data is null)
        {
            return verificationResult;
        }

        var data = verificationResult.Data;

        // Skip processing if there is no customer reference to link to the DB record.
        if (string.IsNullOrWhiteSpace(data.CustomerReference))
        {
            return AppResponses.Success("Webhook verified, but no customer reference was found.", data);
        }

        var paymentRecord = await sharedUnitOfWork.PaymentRecordRepository
            .FirstOrDefaultAsync(r => r.CustomerReference == data.CustomerReference, cancellationToken)
            .ConfigureAwait(false);

        if (paymentRecord is null)
        {
            return AppResponses.Failure<PaymentWebhookVerificationResponse>(
                $"Payment record not found for customer reference: {data.CustomerReference}");
        }

        if (string.Equals(data.EventType, "checkout.session.completed", StringComparison.OrdinalIgnoreCase))
        {
            paymentRecord.UpdateStatus(timeProvider.GetUtcNow(), PaymentStatus.Success, data.PaymentReference);
            await sharedUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (string.Equals(data.EventType, "checkout.session.async_payment_failed", StringComparison.OrdinalIgnoreCase))
        {
            paymentRecord.UpdateStatus(timeProvider.GetUtcNow(), PaymentStatus.Failed, data.PaymentReference);
            await sharedUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        }

        return AppResponses.Success("Webhook processed successfully.", data);
    }
}
