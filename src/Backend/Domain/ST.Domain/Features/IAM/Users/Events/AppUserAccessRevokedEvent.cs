using ST.Domain.Shared.Contracts.Common;

namespace ST.Domain.Features.IAM.Users.Events;

public sealed record AppUserAccessRevokedEvent(string Id, Guid TenantId, string RevokedBy, string RevokeReason, DateTimeOffset OccurredAt) : IDomainEvent;
