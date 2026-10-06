namespace ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;

public sealed record RemoveTenantModuleRequest
{
    public required string ModuleKey { get; init; }
}
