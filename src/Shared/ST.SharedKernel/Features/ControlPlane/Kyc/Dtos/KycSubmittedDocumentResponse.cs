namespace ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

public sealed record KycSubmittedDocumentResponse(
    string DocumentType, string FileName, DateTimeOffset UploadedAt, string Status, string? Feedback)
{
    public string SafetyStatus { get; init; } = "Pending";
    public DateTimeOffset? SafetyCheckedAt { get; init; }
    public string DocumentTypeKey { get; init; } = "legacy";
}
