using ST.Application.Features.Shared.Payments.Contracts.Interfaces;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Payments.Dtos;
using MediatR;

namespace ST.Application.Features.Shared.Payments.QueryHandlers;

internal sealed class GetPaymentStatusHandler(IPaymentGateway paymentGateway)
    : IRequestHandler<GetPaymentStatusQuery, AppResponse<PaymentStatusResponse>>
{
    public Task<AppResponse<PaymentStatusResponse>> Handle(
        GetPaymentStatusQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return paymentGateway.GetStatusAsync(request.PaymentReference, request.Provider, cancellationToken);
    }
}
