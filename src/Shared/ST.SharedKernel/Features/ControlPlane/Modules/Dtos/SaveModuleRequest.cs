namespace ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
public sealed record SaveModuleRequest(string Key, string Name, string Description, bool IsActive = true)
{
    public string[]? DependencyKeys { get; init; }
}
