using ST.SharedKernel.Features.ControlPlane.Tenants;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
namespace ST.UI.Blazor.Features.IAM.Users.Implementations;

public sealed class FrontendTenantContext
{
    public string? Origin
    {
        get; private set;
    }
    public WorkspaceInfoResponse? Workspace
    {
        get; private set;
    }
    public bool IsBound => Workspace is not null;

    public bool Initialize(string baseUri)
    {
        Workspace = null;
        Origin = null;

        if (!Uri.TryCreate(baseUri, UriKind.Absolute, out var uri) || !FrontendOrigin.TryNormalize(uri.GetLeftPart(UriPartial.Authority), out var canonical))
        {
            return false;
        }
        Origin = canonical;
        return true;
    }

    public void Apply(WorkspaceInfoResponse workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        Workspace = workspace.IsFrontendBound ? workspace : null;
    }
}
