using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;

namespace ST.Domain.Features.HR.Employees.Events;

public sealed record EmployeeCreatedEvent(
    Guid EmployeeId,
    string Number,
    string Email,
    string Name

) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
