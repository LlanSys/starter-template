namespace ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

public sealed record KycSubmissionLinkResponse(string Token, DateTimeOffset ExpiresAt);
