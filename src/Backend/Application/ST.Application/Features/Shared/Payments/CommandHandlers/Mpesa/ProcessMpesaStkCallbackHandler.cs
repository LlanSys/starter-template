using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Features.Shared.Payments.Enums;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ST.Application.Features.Shared.Payments.CommandHandlers.Mpesa;

internal sealed class ProcessMpesaStkCallbackHandler(
    ISharedUnitOfWork sharedUnitOfWork,
    ILogger<ProcessMpesaStkCallbackHandler> logger, TimeProvider timeProvider) : IRequestHandler<ProcessMpesaStkCallbackCommand, AppResponse<string>>
{
    public async Task<AppResponse<string>> Handle(ProcessMpesaStkCallbackCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!request.Payload.TryGetProperty("Body", out var bodyEl) ||
                !bodyEl.TryGetProperty("stkCallback", out var stkCallbackEl))
            {
                LogDefinitions.LogMpesaStkInvalidPayload(logger);
                return AppResponses.Failure<string>("Invalid M-Pesa STK callback payload.");
            }

            var checkoutRequestId = stkCallbackEl.TryGetProperty("CheckoutRequestID", out var checkoutIdEl) ? checkoutIdEl.GetString() : null;
            var resultCode = stkCallbackEl.TryGetProperty("ResultCode", out var resultCodeEl) ? resultCodeEl.GetInt32() : -1;
            var resultDesc = stkCallbackEl.TryGetProperty("ResultDesc", out var resultDescEl) ? resultDescEl.GetString() : null;

            if (string.IsNullOrWhiteSpace(checkoutRequestId))
            {
                LogDefinitions.LogMpesaStkInvalidPayload(logger);
                return AppResponses.Failure<string>("CheckoutRequestID is missing.");
            }

            var paymentRecord = await sharedUnitOfWork.PaymentRecordRepository
                .FirstOrDefaultAsync(x => x.ProviderReference == checkoutRequestId, cancellationToken)
                .ConfigureAwait(false);

            if (paymentRecord == null)
            {
                LogDefinitions.LogMpesaStkUnknownCheckoutRequestId(logger, checkoutRequestId);
                return AppResponses.Failure<string>("Payment record not found.");
            }

            if (resultCode == 0)
            {
                paymentRecord.UpdateStatus(timeProvider.GetUtcNow(), PaymentStatus.Success, statusMessage: resultDesc);
            }
            else if (resultCode == 1032)
            {
                paymentRecord.UpdateStatus(timeProvider.GetUtcNow(), PaymentStatus.Cancelled, statusMessage: resultDesc);
            }
            else
            {
                paymentRecord.UpdateStatus(timeProvider.GetUtcNow(), PaymentStatus.Failed, statusMessage: resultDesc);
            }

            await sharedUnitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

            LogDefinitions.LogMpesaStkCallbackProcessed(logger, checkoutRequestId, resultCode);

            return AppResponses.Success("STK callback processed successfully.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogMpesaStkCallbackProcessingError(logger, ex);
            return AppResponses.Failure<string>("An error occurred while processing the callback.");
        }
    }
}
