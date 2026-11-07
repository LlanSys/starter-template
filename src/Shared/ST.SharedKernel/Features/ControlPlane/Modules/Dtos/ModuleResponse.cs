namespace ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
public sealed record ModuleResponse(Guid Id, string Key, string Name, string Description, bool IsActive, bool IsImplemented)
{
    public string[] DependencyKeys { get; init; } = [];
    public bool IsRetired { get; init; }
}
