namespace ST.Application.Features.ControlPlane.Kyc.Contracts;

public interface IKycDocumentSafetyProcessor
{
    Task<int> ProcessBatchAsync(CancellationToken cancellationToken);
}
