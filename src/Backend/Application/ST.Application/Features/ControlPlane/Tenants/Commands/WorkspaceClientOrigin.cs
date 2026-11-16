using ST.SharedKernel.Features.ControlPlane.Tenants;
namespace ST.Application.Features.ControlPlane.Tenants.Commands;

internal static class WorkspaceClientOrigin
{
    public static bool TryNormalize(string? candidate, out string? origin)
    {
        origin = null;
        return candidate is null || FrontendOrigin.TryNormalize(candidate, out origin);
    }
}
