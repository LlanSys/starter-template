namespace ST.Infrastructure.Features.ControlPlane.Kyc;

// Cursor loss on restart causes a safe rescan; upload reservations and erasure commits are durable.
internal sealed class KycOrphanScanProgress
{
    public string? Cursor { get; set; }
}
