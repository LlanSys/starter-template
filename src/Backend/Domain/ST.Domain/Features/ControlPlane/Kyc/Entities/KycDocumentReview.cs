namespace ST.Domain.Features.ControlPlane.Kyc.Entities;

public sealed class KycDocumentReview
{
    private KycDocumentReview() { }

    public KycDocumentReview(Guid documentId, bool accepted, string feedback, string actorId, DateTimeOffset now)
    {
        DocumentId = documentId;
        Accepted = accepted;
        Feedback = feedback;
        ReviewedBy = actorId;
        ReviewedAt = now;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid DocumentId { get; private set; }
    public bool Accepted { get; private set; }
    public string Feedback { get; private set; } = string.Empty;
    public string ReviewedBy { get; private set; } = string.Empty;
    public DateTimeOffset ReviewedAt { get; private set; }
}
