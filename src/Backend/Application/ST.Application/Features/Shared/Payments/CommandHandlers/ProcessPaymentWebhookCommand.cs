using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Payments.Dtos;
using MediatR;

namespace ST.Application.Features.Shared.Payments.CommandHandlers;

public sealed record ProcessPaymentWebhookCommand(
    string Provider,
    string Payload,
    string SignatureHeader) : IRequest<AppResponse<PaymentWebhookVerificationResponse>>;
