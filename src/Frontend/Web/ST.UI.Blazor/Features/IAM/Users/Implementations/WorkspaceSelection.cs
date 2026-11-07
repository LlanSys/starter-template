using ST.UI.Blazor.Features.IAM.Users.Contracts.Interfaces;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using ST.UI.Blazor.Features.IAM.Users.Models;
namespace ST.UI.Blazor.Features.IAM.Users.Implementations;
internal sealed class WorkspaceSelection(ProtectedSessionStorage storage, IServerTokenStore server)
{
    private const string Key = "auth.workspace_selection";
    public async Task<WorkspaceSelectionInfo?> GetAsync()
    {
        var selected = await storage.GetAsync<WorkspaceSelectionInfo>(Key).ConfigureAwait(false);
        return selected.Success ? selected.Value : null;
    }
    public async Task SetAsync(WorkspaceSelectionInfo selected)
    {
        ArgumentNullException.ThrowIfNull(selected);
        var previous = await GetAsync().ConfigureAwait(false);
        await storage.SetAsync(Key, selected).ConfigureAwait(false);
        if (previous?.TenantId != selected.TenantId) await server.ClearAsync().ConfigureAwait(false);
    }
}
