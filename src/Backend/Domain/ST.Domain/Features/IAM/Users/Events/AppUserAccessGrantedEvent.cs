using ST.Domain.Shared.Contracts.Common;

namespace ST.Domain.Features.IAM.Users.Events;

public sealed record AppUserAccessGrantedEvent(string Id, Guid TenantId, string GrantedBy) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
