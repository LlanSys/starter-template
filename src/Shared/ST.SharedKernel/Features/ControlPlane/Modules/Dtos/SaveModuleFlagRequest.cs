namespace ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
public sealed record SaveModuleFlagRequest(Guid TenantId, string Key, string Name, bool IsEnabled, bool UseDefault = false);
