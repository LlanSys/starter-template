using System.Collections.Frozen;
namespace ST.SharedKernel.Features.ControlPlane.Modules;
// A deployment declares code bindings here. Database records can configure them but cannot create executable pages.
public static class DeployedModuleManifest
{
    public static IReadOnlySet<string> Modules { get; } = new[] { "core", "iam", "payments", "showcase" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    public static IReadOnlyDictionary<string, string> Pages { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["/dashboard"] = "core", ["/admin"] = "core", ["/admin/menus"] = "iam", ["/admin/org-settings"] = "core",
        ["/admin/support"] = "iam", ["/admin/iam"] = "iam", ["/admin/access-catalog"] = "iam", ["/admin/settings"] = "iam",
        ["/payments"] = "payments", ["/features/payments"] = "payments", ["/overview"] = "showcase"
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
}
