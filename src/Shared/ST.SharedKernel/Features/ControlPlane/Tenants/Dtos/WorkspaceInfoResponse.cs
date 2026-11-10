namespace ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
public sealed record WorkspaceInfoResponse(Guid TenantId, string Identifier, string Name, string? ClientOrigin = null);
