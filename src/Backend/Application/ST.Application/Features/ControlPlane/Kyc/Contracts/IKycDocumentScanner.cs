namespace ST.Application.Features.ControlPlane.Kyc.Contracts;

public interface IKycDocumentScanner
{
    Task<KycScanResult> ScanAsync(Stream content, CancellationToken cancellationToken);
}
