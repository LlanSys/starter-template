namespace ST.Domain.Features.ControlPlane.Kyc.Entities;

public sealed class KycRetentionPolicy
{
    public static readonly Guid DefaultId = Guid.Parse("019a7420-0000-7000-8000-000000000001");
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = "General Business";
    public int ApprovedMonths { get; set; } = 24;
    public int ClosedDays { get; set; } = 90;
    public int InactiveDays { get; set; } = 30;
    public int RecoveryDays { get; set; } = 30;
    public int OrphanDays { get; set; } = 7;
    public int HoldReviewDays { get; set; } = 90;
    public DateTimeOffset PublishedAt { get; set; }
    public string PublishedBy { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
