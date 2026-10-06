using ST.Domain.Features.Shared.Payments.Entities;
using ST.Application.Features.Shared.Payments.Contracts.Interfaces;
using ST.Infrastructure.Configuration;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Payments.Dtos;
using Microsoft.Extensions.Options;

namespace ST.Infrastructure.Features.Shared.Payments.Contracts.Implementations;

internal sealed class RoutedPaymentGateway(
    IOptions<PaymentSettings> options,
    NoOpPaymentGateway noOpPaymentGateway,
    StripePaymentGateway stripePaymentGateway,
    MpesaPaymentGateway mpesaPaymentGateway) : IPaymentGateway
{
    private readonly PaymentSettings _settings = options.Value;

    public Task<AppResponse<PaymentInitiationResponse>> InitiateAsync(
        PaymentRecord record,
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return ResolveGateway(request.Provider).InitiateAsync(record, request, cancellationToken);
    }

    public Task<AppResponse<PaymentStatusResponse>> GetStatusAsync(
        string paymentReference,
        string? provider = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentReference);

        return ResolveGateway(provider).GetStatusAsync(paymentReference, provider, cancellationToken);
    }

    private IPaymentGateway ResolveGateway(string? requestedProvider)
    {
        var effectiveProvider = string.IsNullOrWhiteSpace(requestedProvider)
            ? _settings.Provider
            : requestedProvider;

        return PaymentProviderParser.Parse(effectiveProvider) switch
        {
            PaymentProviderKind.NoOp => noOpPaymentGateway,
            PaymentProviderKind.Stripe => stripePaymentGateway,
            PaymentProviderKind.Mpesa => mpesaPaymentGateway,
            _ => new UnsupportedPaymentGateway(effectiveProvider)
        };
    }
}
