namespace ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;

public sealed record InspectAdministratorInvitationRequest(Guid InvitationId, string Token);
