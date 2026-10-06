using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Payments.Dtos;
using MediatR;

namespace ST.Application.Features.Shared.Payments.QueryHandlers;

public sealed record GetPaymentStatusQuery(string Provider, string PaymentReference)
    : IRequest<AppResponse<PaymentStatusResponse>>, ICachableRequest
{
    public string CacheGroup => "payments";

    public string Discriminator => CacheKeys.Entity(Provider, PaymentReference);

    public string? CacheUserId => null;

    public bool IsVersioned => false;

    public bool BypassCache => true;
}
