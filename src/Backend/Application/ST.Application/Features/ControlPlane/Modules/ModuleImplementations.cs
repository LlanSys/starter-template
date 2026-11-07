namespace ST.Application.Features.ControlPlane.Modules;
public static class ModuleImplementations
{
    public static bool Contains(string key) => ST.SharedKernel.Features.ControlPlane.Modules.DeployedModuleManifest.Modules.Contains(key);
}
