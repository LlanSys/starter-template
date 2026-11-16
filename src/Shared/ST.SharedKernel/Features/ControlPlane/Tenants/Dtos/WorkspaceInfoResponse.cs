namespace ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;

public sealed record WorkspaceInfoResponse(Guid TenantId, string Identifier, string Name, string? ClientOrigin = null)
{
    public bool IsFrontendBound
    {
        get; init;
    }

    public bool IsPlatformWorkspace
    {
        get; init;
    }
}
