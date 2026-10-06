using ST.Domain.Features.Shared.Payments.Entities;
using ST.Application.Features.Shared.Payments.Contracts.Interfaces;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Payments.Dtos;

namespace ST.Infrastructure.Features.Shared.Payments.Contracts.Implementations;

internal sealed class UnsupportedPaymentGateway : IPaymentGateway
{
    private readonly string? _provider;

    public UnsupportedPaymentGateway(string? provider)
    {
        _provider = provider;
    }

    public Task<AppResponse<PaymentInitiationResponse>> InitiateAsync(
        PaymentRecord record,
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AppResponses.Failure<PaymentInitiationResponse>(
            BuildMessage(request.Provider ?? _provider)));
    }

    public Task<AppResponse<PaymentStatusResponse>> GetStatusAsync(
        string paymentReference,
        string? provider = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AppResponses.Failure<PaymentStatusResponse>(
            BuildMessage(provider ?? _provider)));

    private static string BuildMessage(string? provider) =>
        string.IsNullOrWhiteSpace(provider)
            ? "Payment provider is not configured for this environment."
            : "Selected payment provider is not supported.";
}
