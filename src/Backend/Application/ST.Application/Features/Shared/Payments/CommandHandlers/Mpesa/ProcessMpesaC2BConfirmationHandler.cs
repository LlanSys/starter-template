using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Features.Shared.Payments.Entities;
using ST.Domain.Features.Shared.Payments.Enums;
using ST.Domain.Shared.ValueObjects;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ST.Application.Features.Shared.Payments.CommandHandlers.Mpesa;

internal sealed class ProcessMpesaC2BConfirmationHandler(
    ISharedUnitOfWork sharedUnitOfWork,
    ILogger<ProcessMpesaC2BConfirmationHandler> logger, TimeProvider timeProvider) : IRequestHandler<ProcessMpesaC2BConfirmationCommand, AppResponse<string>>
{
    public async Task<AppResponse<string>> Handle(ProcessMpesaC2BConfirmationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var transId = GetStringValue(request.Payload, "TransID");
            var billRefNumber = GetStringValue(request.Payload, "BillRefNumber");
            var transAmount = GetStringValue(request.Payload, "TransAmount");

            if (string.IsNullOrWhiteSpace(transId) || string.IsNullOrWhiteSpace(transAmount))
            {
                LogDefinitions.LogMpesaC2bInvalidPayload(logger);
                return AppResponses.Failure<string>("Invalid C2B confirmation payload.");
            }

            LogDefinitions.LogMpesaC2bConfirmationReceived(logger, transId, billRefNumber ?? "Unknown");

            if (!decimal.TryParse(transAmount, out var amountValue))
            {
                amountValue = 0;
            }

            // In C2B, the BillRefNumber is usually what the customer typed as the Account Number.
            // Check if we have a pending PaymentRecord with this CustomerReference
            var paymentRecord = await sharedUnitOfWork.PaymentRecordRepository
                .FirstOrDefaultAsync(x => x.CustomerReference == billRefNumber && x.Status == PaymentStatus.Initiated, cancellationToken)
                .ConfigureAwait(false);

            if (paymentRecord != null)
            {
                // Update existing record
                paymentRecord.UpdateStatus(timeProvider.GetUtcNow(), PaymentStatus.Success, transId);
            }
            else
            {
                // Unsolicited C2B payment (e.g. they just sent money to the PayBill without checking out first)
                var money = new Money(amountValue, "KES");
                var newRecord = new PaymentRecord(
                    id: Guid.CreateVersion7(),
                    amount: money,
                    description: "M-Pesa C2B Payment",
                    customerReference: string.IsNullOrWhiteSpace(billRefNumber) ? transId : billRefNumber,
                    provider: "Mpesa",
                    idempotencyKey: transId, // Use TransID as idempotency to avoid duplicates
                    status: PaymentStatus.Success)
                {
                    CreatedBy = "MpesaC2BWebhook"
                };

                // Explicitly set the provider reference
                newRecord.UpdateStatus(timeProvider.GetUtcNow(), PaymentStatus.Success, transId);

                await sharedUnitOfWork.PaymentRecordRepository.CreateAsync(newRecord, cancellationToken).ConfigureAwait(false);
            }

            await sharedUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

            return AppResponses.Success("C2B confirmation processed successfully.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogMpesaC2bConfirmationError(logger, ex);
            return AppResponses.Failure<string>("An error occurred while processing the C2B confirmation.");
        }
    }

    private static string? GetStringValue(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) ||
            element.TryGetProperty(propertyName.ToLowerInvariant(), out prop) ||
            element.TryGetProperty(char.ToLowerInvariant(propertyName[0]) + propertyName[1..], out prop))
        {
            return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.GetRawText();
        }
        return null;
    }
}
