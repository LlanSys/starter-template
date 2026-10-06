using ST.Domain.Shared.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Features.HR.Employees.IntegrationEvents;

public sealed record EmployeeCreatedIntegrationEvent(
    Guid EmployeeId,
    string Number,
    string Name,
    string Email,
    string Type) : IIntegrationEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
