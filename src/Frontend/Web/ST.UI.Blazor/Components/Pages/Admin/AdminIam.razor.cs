using Microsoft.AspNetCore.Components;
using MudBlazor;
using ST.UI.Blazor.Features.IAM.Permissions.Components;

namespace ST.UI.Blazor.Components.Pages.Admin;

public partial class AdminIam : IDisposable
{
    private static readonly (string Key, string Title, string Permission)[] Tabs =
    [
        ("roles", "Roles", "roles.view"),
        ("permissions", "Permissions", "roles.manage_permissions"),
        ("users", "Users", "users.view"),
        ("devices", "User Devices", "users.view")
    ];
    private string _selection = "roles";
    private int _roleRevision;
    private AdminRoles? _rolesTab;
    private AdminPermissions? _permissionsTab;
    private AdminUsers? _usersTab;
    private bool _disposed;
    private (string Key, string Title, string Permission)[] VisibleTabs => Tabs.Where(tab => AuthSession.HasPermission(tab.Permission)).ToArray();
    private string ActiveTab => VisibleTabs.Any(tab => tab.Key == _selection) ? _selection : VisibleTabs.FirstOrDefault().Key ?? string.Empty;
    private int ActiveIndex => Math.Max(0, Array.FindIndex(VisibleTabs, tab => tab.Key == ActiveTab));

    protected override void OnInitialized()
    {
        AuthSession.AuthenticationChanged += OnAuthenticationChanged;
    }

    private void OnTabChanged(int index)
    {
        var tabs = VisibleTabs;

        if (index >= 0 && index < tabs.Length)
        {
            _selection = tabs[index].Key;
        }
    }

    private void OnAuthenticationChanged()
    {
        _ = InvokeAsync(() =>
        {
            if (_disposed)
            {
                return;
            }
            _selection = ActiveTab;
            StateHasChanged();
        });
    }

    private async Task ApplyTemplateAsync()
    {
        var dialog = await DialogService.ShowAsync<RoleTemplateDialog>("Apply Role Template",
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true });

        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            _roleRevision++;
        }
    }

    private Task OpenRoleCreateDialogAsync()
    {
        return _rolesTab?.OpenCreateDialogAsync() ?? Task.CompletedTask;
    }

    private Task OpenPermissionCreateDialogAsync()
    {
        return _permissionsTab?.OpenCreateDialogAsync() ?? Task.CompletedTask;
    }

    private Task OpenUserCreateDialogAsync()
    {
        return _usersTab?.OpenCreateDialogAsync() ?? Task.CompletedTask;
    }

    public void Dispose()
    {
        _disposed = true;
        AuthSession.AuthenticationChanged -= OnAuthenticationChanged;
    }
}
