namespace ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

public sealed record KycRetentionPolicyResponse(Guid Id, string Name, int ApprovedMonths, int ClosedDays,
    int InactiveDays, int RecoveryDays, int OrphanDays, int HoldReviewDays, DateTimeOffset PublishedAt,
    string PublishedBy, string Reason);
