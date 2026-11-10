namespace ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
public sealed record InitializeWorkspaceRequest(string AdministratorEmail, string FirstName, string LastName, bool SendInvitation = true);
