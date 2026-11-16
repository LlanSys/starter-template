using ST.Domain.Features.ControlPlane.Kyc.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

namespace ST.Application.Features.ControlPlane.Kyc.Contracts;

public interface IKycRetentionService
{
    Task<AppResponse<bool>> CheckErasureAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<KycRetentionCase> EnsureAsync(Guid tenantId, bool approved, CancellationToken cancellationToken);
    Task<AppResponse<KycRetentionStatusResponse>> StatusAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<AppResponse<IReadOnlyList<KycRetentionPolicyResponse>>> PoliciesAsync(CancellationToken cancellationToken);
    Task<AppResponse<Guid>> PublishAsync(PublishKycRetentionPolicyRequest request, CancellationToken cancellationToken);
    Task<AppResponse<bool>> ActAsync(Guid tenantId, KycRetentionActionRequest request, CancellationToken cancellationToken);
    Task ProcessAsync(Guid tenantId, CancellationToken cancellationToken);
    Task ReconcileOrphansAsync(CancellationToken cancellationToken);
}
