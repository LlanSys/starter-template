using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Features.Banking.Customers.Events;

public sealed record CustomerLinkedToUserEvent(string Id, Guid CustomerId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
