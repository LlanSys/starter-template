using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Payments.Dtos;

using MediatR;

namespace ST.Application.Features.Shared.Payments.QueryHandlers;

public sealed record GetPaymentCapabilitiesQuery
    : IRequest<AppResponse<IReadOnlyCollection<PaymentProviderCapabilityResponse>>>, ICachableRequest
{
    public string CacheGroup => "payments";
    public string Discriminator => "capabilities";
    public string? CacheUserId => null;
    public bool IsVersioned => false;
    public bool BypassCache => true;
}
