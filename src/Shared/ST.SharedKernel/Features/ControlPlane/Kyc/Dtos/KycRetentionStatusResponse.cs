namespace ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

public sealed record KycRetentionStatusResponse(Guid TenantId, Guid PolicyId, Guid Revision, string Outcome,
    string Phase, DateTimeOffset LastActivityAt, DateTimeOffset? ClosedAt, DateTimeOffset? RetainUntil,
    DateTimeOffset? RecoveryUntil, bool Held, DateTimeOffset? HoldReviewDue, string? FailureCode,
    DateTimeOffset? NextAttemptAt);
