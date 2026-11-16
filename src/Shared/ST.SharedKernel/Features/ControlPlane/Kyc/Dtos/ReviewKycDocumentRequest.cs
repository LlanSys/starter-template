namespace ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

public sealed record ReviewKycDocumentRequest(bool Accepted, string Feedback);
