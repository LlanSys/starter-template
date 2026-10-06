using ST.SharedKernel.Features.Shared.Payments.Dtos;

namespace ST.Application.Features.Shared.Payments.Contracts.Interfaces;

public interface IPaymentProviderCatalog
{
    IReadOnlyCollection<PaymentProviderCapabilityResponse> GetCapabilities();
}
