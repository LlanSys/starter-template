using ST.Domain.Features.IAM.Users.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;

namespace ST.Application.Features.ControlPlane.Tenants.Contracts;

public interface IAdministratorInvitationService
{
    Task<AppResponse<AdministratorInvitationPreview>> InspectAsync(InspectAdministratorInvitationRequest request, CancellationToken cancellationToken);
    Task<AppResponse<bool>> AcceptPasswordAsync(AcceptAdministratorInvitationRequest request, CancellationToken cancellationToken);
    Task<AppResponse<AppUser>> AcceptExternalAsync(Guid invitationId, string token, string provider, string providerKey, string email, CancellationToken cancellationToken);
    Task<AppResponse<bool>> CompleteAsync(CancellationToken cancellationToken);
    Task<AppResponse<bool>> ChangeDeliveryAsync(Guid tenantId, bool revoke, CancellationToken cancellationToken);
}
