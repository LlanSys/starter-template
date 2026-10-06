using ST.Application.Features.Shared.Payments.Contracts.Interfaces;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Payments.Dtos;

using MediatR;

namespace ST.Application.Features.Shared.Payments.QueryHandlers;

internal sealed class GetPaymentCapabilitiesHandler(IPaymentProviderCatalog providerCatalog)
    : IRequestHandler<GetPaymentCapabilitiesQuery, AppResponse<IReadOnlyCollection<PaymentProviderCapabilityResponse>>>
{
    public Task<AppResponse<IReadOnlyCollection<PaymentProviderCapabilityResponse>>> Handle(
        GetPaymentCapabilitiesQuery request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(AppResponses.Success(providerCatalog.GetCapabilities()));
    }
}
