using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Features.IAM.Users.Events;

public sealed record AppUserCreatedEvent(
    string UserId,
    Guid TenantId,
    Guid? EmployeeId,
    string UserName,
    string FullName,
    string Email) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
