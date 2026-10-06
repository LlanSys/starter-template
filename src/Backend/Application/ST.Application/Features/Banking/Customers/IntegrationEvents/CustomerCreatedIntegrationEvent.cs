using ST.Application.Contracts.Interfaces.Common;
using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Features.Banking.Customers.IntegrationEvents;

public record CustomerCreatedIntegrationEvent(
    Guid CustomerId,
    string Number,
    string Name,
    string Email,
    string Type) : IIntegrationEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
