namespace ST.Application.Features.ControlPlane.Kyc.Contracts;

public interface IKycEvidenceLifecycleStorage
{
    Task<bool> IsErasureRecordedAsync(string key, CancellationToken cancellationToken);
    Task RecordErasureAsync(string key, CancellationToken cancellationToken);
    Task<bool> PurgeAsync(string key, CancellationToken cancellationToken);
    Task<KycStoragePage> ListAsync(string? cursor, CancellationToken cancellationToken);
}
