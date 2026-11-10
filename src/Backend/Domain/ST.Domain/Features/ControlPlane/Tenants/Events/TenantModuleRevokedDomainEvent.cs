using System;
using ST.Domain.Shared.Contracts.Common;

namespace ST.Domain.Features.ControlPlane.Tenants.Events;

public record TenantModuleRevokedDomainEvent(
    Guid TenantId,
    string ModuleKey,
    DateTimeOffset OccurredAt) : IDomainEvent;
