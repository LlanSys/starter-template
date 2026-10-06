using ST.Domain.Shared.Contracts.Common;

namespace ST.Domain.Features.IAM.Users.Events;

public sealed record AppUserAccessRevokedEvent(string Id, Guid TenantId, string RevokedBy, string RevokeReason) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
