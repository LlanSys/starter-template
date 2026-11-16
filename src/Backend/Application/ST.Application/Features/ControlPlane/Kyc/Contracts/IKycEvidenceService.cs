using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

namespace ST.Application.Features.ControlPlane.Kyc.Contracts;

public interface IKycEvidenceService
{
    Task<AppResponse<KycCasePageResponse>> CasesAsync(string? search, int page, CancellationToken cancellationToken);
    Task<AppResponse<KycCaseSummaryResponse>> CaseAsync(Guid tenantId, CancellationToken cancellationToken);

    Task<AppResponse<IReadOnlyList<KycDocumentResponse>>> ListAsync(Guid tenantId, CancellationToken cancellationToken, string? documentTypeKey = null);
    Task<AppResponse<KycSubmissionLinkResponse>> IssueLinkAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<AppResponse<bool>> RevokeLinkAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<AppResponse<KycSubmissionStatusResponse>> InspectLinkAsync(Guid tenantId, string token, CancellationToken cancellationToken);
    Task<AppResponse<Guid>> UploadAsync(Guid tenantId, string? token, string documentType,
        string fileName, long length, Stream content, CancellationToken cancellationToken, string? otherDocumentName = null);
    Task<AppResponse<bool>> RetrySafetyScanAsync(Guid tenantId, Guid documentId, RetryKycSafetyScanRequest request, CancellationToken cancellationToken);
    Task<AppResponse<bool>> ReviewAsync(Guid tenantId, Guid documentId, ReviewKycDocumentRequest request, CancellationToken cancellationToken);
    Task<AppResponse<KycDocumentDownload>> DownloadAsync(Guid tenantId, Guid documentId, CancellationToken cancellationToken);
}
