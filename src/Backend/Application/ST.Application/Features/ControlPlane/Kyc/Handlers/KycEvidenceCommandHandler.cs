using MediatR;
using ST.Application.Features.ControlPlane.Kyc.Commands;
using ST.Application.Features.ControlPlane.Kyc.Contracts;
using ST.SharedKernel.Dtos.Common;

namespace ST.Application.Features.ControlPlane.Kyc.Handlers;

internal sealed class KycEvidenceCommandHandler(IKycEvidenceService evidence)
    : IRequestHandler<UploadKycDocumentCommand, AppResponse<Guid>>,
      IRequestHandler<ReviewKycDocumentCommand, AppResponse<bool>>,
      IRequestHandler<RetryKycSafetyScanCommand, AppResponse<bool>>
{
    public Task<AppResponse<Guid>> Handle(UploadKycDocumentCommand request, CancellationToken cancellationToken)
    {
        return evidence.UploadAsync(request.TenantId, request.Token, request.DocumentType,
            request.FileName, request.Length, request.Content, cancellationToken, request.OtherDocumentName);
    }

    public Task<AppResponse<bool>> Handle(ReviewKycDocumentCommand request, CancellationToken cancellationToken)
    {
        return evidence.ReviewAsync(request.TenantId, request.DocumentId, request.Request, cancellationToken);
    }
    public Task<AppResponse<bool>> Handle(RetryKycSafetyScanCommand request, CancellationToken cancellationToken)
    {
        return evidence.RetrySafetyScanAsync(request.TenantId, request.DocumentId, request.Request, cancellationToken);
    }
}
