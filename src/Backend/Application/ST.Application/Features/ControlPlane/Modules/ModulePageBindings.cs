namespace ST.Application.Features.ControlPlane.Modules;
public static class ModulePageBindings
{
    public static bool IsImplemented(string path, string module) => ST.SharedKernel.Features.ControlPlane.Modules.DeployedModuleManifest.Pages.TryGetValue(path, out var owner) && string.Equals(owner, module, StringComparison.OrdinalIgnoreCase);
    public static string? Owner(string path) => ST.SharedKernel.Features.ControlPlane.Modules.DeployedModuleManifest.Pages.GetValueOrDefault(path);
}
