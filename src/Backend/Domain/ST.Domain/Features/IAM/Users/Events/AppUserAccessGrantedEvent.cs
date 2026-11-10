using ST.Domain.Shared.Contracts.Common;

namespace ST.Domain.Features.IAM.Users.Events;

public sealed record AppUserAccessGrantedEvent(string Id, Guid TenantId, string GrantedBy, DateTimeOffset OccurredAt) : IDomainEvent;
