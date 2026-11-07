namespace ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
public sealed record WorkspaceSetupResponse(Guid TenantId, int Version, string? AdministratorEmail, bool InvitationSent, bool EmailConfirmed, bool MfaEnabled, string FirstName = "", string LastName = "");
