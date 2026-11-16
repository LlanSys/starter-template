using MediatR;
using ST.SharedKernel.Dtos.Common;

namespace ST.Application.Features.ControlPlane.Kyc.Commands;

public sealed record UploadKycDocumentCommand(
    Guid TenantId, string? Token, string DocumentType, string FileName, long Length, Stream Content, string? OtherDocumentName = null)
    : IRequest<AppResponse<Guid>>
{
    public override string ToString()
    {
        return nameof(UploadKycDocumentCommand);
    }
}
