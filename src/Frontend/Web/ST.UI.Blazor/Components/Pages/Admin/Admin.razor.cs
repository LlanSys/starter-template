using MudBlazor;
using ST.SharedKernel.Features.IAM.Menus.Dtos;

namespace ST.UI.Blazor.Components.Pages.Admin;

public partial class Admin : IDisposable
{
    private static string MenuTitle(MenuResponse item)
    {
        return item.Url.Equals("/admin/support", StringComparison.OrdinalIgnoreCase)
            && item.Title.Equals("Support access", StringComparison.OrdinalIgnoreCase)
            ? "Support Access"
            : item.Title;
    }

    private readonly List<MenuResponse> _modules = [];
    private bool _isReady;
    private string _accessSignature = string.Empty;
    private bool _disposed;
    private int _loadVersion;

    protected override void OnInitialized()
    {
        AuthSession.AuthenticationChanged += OnAuthenticationChanged;
    }

    private void OnAuthenticationChanged()
    {
        _ = InvokeAsync(async () =>
        {
            if (_disposed)
            {
                return;
            }
            var signature = AccessSignature();

            if (string.Equals(_accessSignature, signature, StringComparison.Ordinal))
            {
                return;
            }
            _accessSignature = signature;
            _modules.Clear();
            _isReady = false;
            StateHasChanged();

            if (AuthSession.IsAuthenticated)
            {
                await LoadModulesAsync();
                StateHasChanged();
            }
        });
    }

    public void Dispose()
    {
        _disposed = true;
        ++_loadVersion;
        AuthSession.AuthenticationChanged -= OnAuthenticationChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await AuthSession.InitializeAsync();

        if (AuthSession.IsAuthenticated)
        {
            _accessSignature = AccessSignature();
            await LoadModulesAsync();
            StateHasChanged();
        }
    }

    private async Task LoadModulesAsync()
    {
        var version = ++_loadVersion;
        var response = await IamAdminService.GetNavigationMenusAsync("AdminCenter");

        if (_disposed || version != _loadVersion)
        {
            return;
        }

        if (!response.IsSuccess || response.Data is null)
        {
            Snackbar.Add(response.Message ?? "We could not load admin menus.", Severity.Error);
            _isReady = true;
            return;
        }

        _modules.Clear();
        var allMenus = Flatten(response.Data);
        _modules.AddRange(allMenus);
        _isReady = true;
    }

    private static IEnumerable<MenuResponse> Flatten(IEnumerable<MenuResponse>? menus)
    {
        if (menus is null)
        {
            yield break;
        }

        foreach (var menu in menus)
        {
            yield return menu;

            foreach (var child in Flatten(menu.Children))
            {
                yield return child;
            }
        }
    }

    private string AccessSignature()
    {
        var user = AuthSession.CurrentUser;

        if (!AuthSession.IsAuthenticated || user is null)
        {
            return string.Empty;
        }
        return string.Join("|", user.AppUserId, user.TenantId, user.SessionId, user.IsPlatformStaff, user.HasMfaProof,
            string.Join(",", user.Roles.Order(StringComparer.Ordinal)),
            string.Join(",", (user.Permissions ?? []).Order(StringComparer.Ordinal)),
            string.Join(",", user.UnavailablePermissions.Order(StringComparer.Ordinal)),
            string.Join(",", user.AvailablePages.Order(StringComparer.Ordinal)));
    }

}
