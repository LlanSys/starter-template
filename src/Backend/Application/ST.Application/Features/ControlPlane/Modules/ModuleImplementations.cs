using ST.SharedKernel.Features.ControlPlane.Modules;

namespace ST.Application.Features.ControlPlane.Modules;
public static class ModuleImplementations
{
    public static bool Contains(string key) => DeployedModuleManifest.Modules.Contains(key);
}
