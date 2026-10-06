using ST.Domain.Features.Shared.Payments.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Payments.Dtos;

namespace ST.Application.Features.Shared.Payments.Contracts.Interfaces;

public interface IPaymentGateway
{
    Task<AppResponse<PaymentInitiationResponse>> InitiateAsync(
        PaymentRecord record,
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default);

    Task<AppResponse<PaymentStatusResponse>> GetStatusAsync(
        string paymentReference,
        string? provider = null,
        CancellationToken cancellationToken = default);
}
