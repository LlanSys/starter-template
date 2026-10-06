using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Features.HR.Employees.Events;

public sealed record EmployeeLinkedToUserEvent(string Id, Guid EmployeeId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
