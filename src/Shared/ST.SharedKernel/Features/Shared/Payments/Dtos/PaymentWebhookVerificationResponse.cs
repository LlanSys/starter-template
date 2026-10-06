namespace ST.SharedKernel.Features.Shared.Payments.Dtos;

public sealed record PaymentWebhookVerificationResponse(
    string Provider,
    string EventId,
    string EventType,
    string CustomerReference,
    string PaymentReference,
    string Status);
