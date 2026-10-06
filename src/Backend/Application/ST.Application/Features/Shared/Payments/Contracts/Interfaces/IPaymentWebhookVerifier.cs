using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Payments.Dtos;

namespace ST.Application.Features.Shared.Payments.Contracts.Interfaces;

public interface IPaymentWebhookVerifier
{
    Task<AppResponse<PaymentWebhookVerificationResponse>> VerifyAsync(
        string provider,
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken = default);
}
