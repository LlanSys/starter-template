namespace ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;

public sealed record AdministratorInvitationPreview(string WorkspaceName, string Email, bool ExistingAccount, DateTimeOffset ExpiresAt);
