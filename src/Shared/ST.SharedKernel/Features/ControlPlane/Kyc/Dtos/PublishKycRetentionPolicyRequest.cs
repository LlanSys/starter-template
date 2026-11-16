namespace ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

public sealed record PublishKycRetentionPolicyRequest(Guid ExpectedPolicyId, string Name, int ApprovedMonths,
    int ClosedDays, int InactiveDays, int RecoveryDays, int OrphanDays, int HoldReviewDays, string Reason);
