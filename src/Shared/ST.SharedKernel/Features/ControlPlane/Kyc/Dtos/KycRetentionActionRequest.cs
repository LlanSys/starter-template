namespace ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

public sealed record KycRetentionActionRequest(Guid ExpectedRevision, string Action, string Reason);
