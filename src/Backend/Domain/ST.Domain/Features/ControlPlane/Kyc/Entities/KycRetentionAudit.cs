namespace ST.Domain.Features.ControlPlane.Kyc.Entities;

public sealed class KycRetentionAudit
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActorId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public Guid PolicyId { get; set; }
}
