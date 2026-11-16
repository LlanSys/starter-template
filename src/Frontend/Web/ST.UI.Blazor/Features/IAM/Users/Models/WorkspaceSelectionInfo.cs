using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
namespace ST.UI.Blazor.Features.IAM.Users.Models;

public sealed record WorkspaceSelectionInfo(Guid TenantId, string Identifier, string Name)
{
    // Presentation metadata only; authorization continues to use the live API identity.
    public bool IsPlatformWorkspace
    {
        get; init;
    }

    public static WorkspaceSelectionInfo From(WorkspaceInfoResponse workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        return new(workspace.TenantId, workspace.Identifier, workspace.Name)
        {
            IsPlatformWorkspace = workspace.IsPlatformWorkspace
        };
    }
}
