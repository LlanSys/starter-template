namespace ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

public sealed record KycSubmissionStatusResponse(
    string WorkspaceName, DateTimeOffset ExpiresAt, long MaxFileBytes, int RemainingUploads,
    IReadOnlyList<KycSubmittedDocumentResponse> Documents)
{
    public IReadOnlyList<KycDocumentTypeOption> DocumentTypes { get; init; } = [];
}
