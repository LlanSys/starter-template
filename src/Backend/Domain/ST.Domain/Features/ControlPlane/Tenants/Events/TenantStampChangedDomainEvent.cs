using System;
using ST.Domain.Shared.Contracts.Common;

namespace ST.Domain.Features.ControlPlane.Tenants.Events;

public record TenantStampChangedDomainEvent(
    Guid TenantId,
    Guid? OldStampId,
    Guid NewStampId,
    DateTimeOffset OccurredAt) : IDomainEvent;
