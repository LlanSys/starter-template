using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;
using ST.SharedKernel;

namespace ST.UI.Blazor.Components.Layout;

public partial class UserProfileMenu
{

    private string DisplayName
    {
        get
        {
            var fullName = $"{AuthSession.CurrentUser?.FirstName} {AuthSession.CurrentUser?.LastName}".Trim();

            return string.IsNullOrWhiteSpace(fullName)
                ? AuthSession.CurrentUser?.Email ?? "Signed-in user"
                : fullName;
        }
    }

    private bool _sessionRestoreFailed;

    private string Initials
    {
        get
        {
            var first = AuthSession.CurrentUser?.FirstName;
            var last = AuthSession.CurrentUser?.LastName;

            if (!string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(last))
            {
                return $"{first[0]}{last[0]}".ToUpperInvariant();
            }

            var email = AuthSession.CurrentUser?.Email;
            return string.IsNullOrWhiteSpace(email) ? ProductIdentity.Prefix : email[..1].ToUpperInvariant();
        }
    }

    private string? ProfilePictureSource => AuthSession.ProfilePictureDataUri;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            await AuthSession.InitializeAsync().WaitAsync(TimeSpan.FromSeconds(8), Clock);
        }
        catch (TimeoutException ex)
        {
            _sessionRestoreFailed = true;
            UserProfileMenuLogDefinitions.LogSessionRestoreTimedOut(Logger, ex);
        }
        catch (Exception ex)
        {
            _sessionRestoreFailed = true;
            UserProfileMenuLogDefinitions.LogSessionRestoreFailed(Logger, ex);
        }
        finally
        {
            StateHasChanged();
        }
    }

    private async Task LogoutAsync()
    {
        var wasDemo = AuthSession.CurrentUser?.IsDemoWorkspace == true;
        await AuthService.LogoutAsync();

        if (wasDemo)
        {
            await AuthService.LogoutAsync();
        }

        await WorkspaceStorage.DeleteAsync("workspace.platform");
        Navigation.NavigateTo("/iam/sign-in", forceLoad: true);
    }

    private MudMenu? _menu;
    private bool _disposed;

    protected override void OnInitialized()
    {
        Navigation.LocationChanged += OnLocationChanged;
        AuthSession.AuthenticationChanged += OnAuthenticationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        _ = InvokeAsync(CloseMenuAsync);
    }

    private void OnAuthenticationChanged()
    {
        if (!_disposed)
        {
            _ = InvokeAsync(StateHasChanged);
        }
    }

    private async Task CloseMenuAsync()
    {
        if (!_disposed && _menu is not null)
        {
            await _menu.CloseMenuAsync();
        }
    }

    public void Dispose()
    {
        _disposed = true;
        Navigation.LocationChanged -= OnLocationChanged;
        AuthSession.AuthenticationChanged -= OnAuthenticationChanged;
    }

}
