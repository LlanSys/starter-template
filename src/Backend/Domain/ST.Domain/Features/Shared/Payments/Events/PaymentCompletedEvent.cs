using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Shared.ValueObjects;
using MediatR;

namespace ST.Domain.Features.Shared.Payments.Events;

public sealed record PaymentCompletedEvent(
    Guid PaymentRecordId,
    string CustomerReference,
    string Provider,
    Money Amount) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
