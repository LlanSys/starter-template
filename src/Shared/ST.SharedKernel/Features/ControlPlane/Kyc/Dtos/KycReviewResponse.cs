namespace ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

public sealed record KycReviewResponse(bool Accepted, string Feedback, string ReviewedBy, DateTimeOffset ReviewedAt);
