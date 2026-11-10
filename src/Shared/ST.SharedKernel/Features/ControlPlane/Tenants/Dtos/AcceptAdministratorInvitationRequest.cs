namespace ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;

public sealed record AcceptAdministratorInvitationRequest(Guid InvitationId, string Token, string Password);
