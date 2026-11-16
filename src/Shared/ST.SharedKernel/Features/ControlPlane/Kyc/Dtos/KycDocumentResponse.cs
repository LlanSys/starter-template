namespace ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

public sealed record KycDocumentResponse(
    Guid Id, string DocumentType, string FileName, string ContentType, long Length,
    DateTimeOffset UploadedAt, string UploadedBy, string Status, string Sha256,
    IReadOnlyList<KycReviewResponse> Reviews)
{
    public Guid Revision { get; init; }
    public string SafetyStatus { get; init; } = "Pending";
    public DateTimeOffset? SafetyCheckedAt { get; init; }
    public string DocumentTypeKey { get; init; } = "legacy";
}
