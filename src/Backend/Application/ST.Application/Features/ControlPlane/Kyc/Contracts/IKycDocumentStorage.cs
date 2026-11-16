namespace ST.Application.Features.ControlPlane.Kyc.Contracts;

public interface IKycDocumentStorage
{
    Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken);
    Task<Stream?> OpenAsync(string key, CancellationToken cancellationToken);
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
