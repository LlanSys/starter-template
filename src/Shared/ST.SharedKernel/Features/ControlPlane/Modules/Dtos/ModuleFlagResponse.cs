namespace ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
public sealed record ModuleFlagResponse(Guid TenantId, string ModuleKey, string Key, string Name, bool IsEnabled);
