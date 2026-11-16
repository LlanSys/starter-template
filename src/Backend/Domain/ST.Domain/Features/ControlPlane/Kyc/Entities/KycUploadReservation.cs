namespace ST.Domain.Features.ControlPlane.Kyc.Entities;

public sealed class KycUploadReservation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public DateTimeOffset LeaseUntil { get; set; }
    public string State { get; set; } = "Uploading";
    public Guid Revision { get; set; } = Guid.CreateVersion7();
}
