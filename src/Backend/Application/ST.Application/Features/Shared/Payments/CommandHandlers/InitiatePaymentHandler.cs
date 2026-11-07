using ST.Application.Features.Shared.Payments.Contracts.Interfaces;
using ST.Domain.Features.Shared.Contracts;
using ST.Domain.Features.Shared.Payments.Entities;
using ST.Domain.Features.Shared.Payments.Enums;
using ST.Domain.Shared.ValueObjects;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Payments.Dtos;

using MediatR;

using System.Globalization;

namespace ST.Application.Features.Shared.Payments.CommandHandlers;

internal sealed class InitiatePaymentHandler(
    IPaymentGateway paymentGateway,
    ISharedUnitOfWork sharedUnitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<InitiatePaymentCommand, AppResponse<PaymentInitiationResponse>>
{
    public async Task<AppResponse<PaymentInitiationResponse>> Handle(
        InitiatePaymentCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var req = request.Request;
        var provider = req.Provider ?? "Unknown";
        var idempotencyKey = string.IsNullOrWhiteSpace(req.IdempotencyKey)
            ? null
            : req.IdempotencyKey.Trim();

        if (idempotencyKey is not null)
        {
            var existingRecord = await sharedUnitOfWork.PaymentRecordRepository
                .FirstOrDefaultAsync(record => record.IdempotencyKey == idempotencyKey, cancellationToken)
                .ConfigureAwait(false);

            if (existingRecord is not null)
            {
                if (!IsMatchingRequest(existingRecord, req, provider))
                {
                    return AppResponses.Failure<PaymentInitiationResponse>(
                        "The idempotency key has already been used for a different payment request.");
                }

                return AppResponses.Success(
                    "The existing payment request was returned.",
                    new PaymentInitiationResponse(
                        existingRecord.Provider,
                        existingRecord.CustomerReference,
                        existingRecord.CheckoutUrl ?? string.Empty,
                        existingRecord.Status.ToString()));
            }
        }

        var providerPrefix = provider.Length >= 3 ? provider[..3].ToUpperInvariant() : provider.ToUpperInvariant();
        var paymentId = Guid.CreateVersion7();

        // Extract a human-readable, collision-resistant reference using the current date and the Guid's entropy
        // Format example: ST-STR-USD-260709-A1B2C3
        var timestamp = timeProvider.GetUtcNow().ToString("yyMMdd", CultureInfo.InvariantCulture);
        var uniqueSuffix = paymentId.ToString("N")[^6..].ToUpperInvariant();
        var customerReference = $"ST-{providerPrefix}-{req.Currency}-{timestamp}-{uniqueSuffix}";

        var money = new Money(request.Request.Amount, request.Request.Currency);
        var paymentRecord = new PaymentRecord(

            id: paymentId,
            amount: money,
            description: request.Request.Description,
            customerReference: customerReference,
            provider: provider,
            idempotencyKey: idempotencyKey)
        {
            // CreatedBy is populated by the audit interceptor in SharedDBContext.SaveChangesAsync
            // via ICurrentActorProvider; defaults to "System" for unauthenticated flows.
            CreatedBy = string.Empty
        };

        // First, persist the initial payment record to ensure we have a reference.
        await sharedUnitOfWork.PaymentRecordRepository.CreateAsync(paymentRecord, cancellationToken).ConfigureAwait(false);
        await sharedUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

        var initiatePaymentResult = await paymentGateway
            .InitiateAsync(paymentRecord, request.Request, cancellationToken)
            .ConfigureAwait(false);

        if (initiatePaymentResult.IsSuccess)
        {
            paymentRecord.SetCheckoutUrl(initiatePaymentResult.Data?.CheckoutUrl);
            paymentRecord.SetProviderReference(initiatePaymentResult.Data?.PaymentReference);
            await sharedUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // If initiation fails, update the record status to Failed.
            paymentRecord.UpdateStatus(timeProvider.GetUtcNow(), PaymentStatus.Failed, statusMessage: initiatePaymentResult.Message);
            await sharedUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
        }

        return initiatePaymentResult;
    }

    private static bool IsMatchingRequest(
        PaymentRecord record,
        PaymentInitiationRequest request,
        string provider) =>
        record.Amount.Amount == request.Amount &&
        string.Equals(record.Amount.Currency, request.Currency, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(record.Provider, provider, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(record.Description, request.Description, StringComparison.Ordinal);
}
