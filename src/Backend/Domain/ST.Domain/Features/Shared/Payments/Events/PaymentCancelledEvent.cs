using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Shared.ValueObjects;

namespace ST.Domain.Features.Shared.Payments.Events;

public sealed record PaymentCancelledEvent(
    Guid PaymentRecordId,
    string CustomerReference,
    string Provider,
    Money Amount,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;
