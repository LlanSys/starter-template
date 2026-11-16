using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.ControlPlane.Staff;
using ST.UI.Blazor.Components.Common;
using ST.UI.Blazor.Features.ControlPlane.Staff.Components;
using ST.UI.Blazor.Features.IAM.Users.Contracts.Interfaces;
using ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;
using ST.UI.Blazor.Features.Shared.Messaging;

namespace ST.UI.Blazor.Features.ControlPlane.Staff.Pages;

public partial class PlatformStaff : IDisposable
{
    [Inject]
    private IBackendApiClient Api { get; set; } = default!;

    [Inject]
    private IDialogService Dialogs { get; set; } = default!;

    [Inject]
    private IAuthSession AuthSession { get; set; } = default!;

    private readonly CancellationTokenSource _lifetime = new();
    private IReadOnlyList<PlatformStaffResponse> _items = [];
    private string _search = string.Empty;
    private string? _message;
    private bool _loading = true;
    private int _page = 1;
    private int _total;
    private int _loadVersion;
    private bool _disposed;
    private IDialogReference? _openDialog;
    private string _authority = string.Empty;
    protected override async Task OnInitializedAsync()
    {
        _authority = AuthorityFingerprint();
        AuthSession.AuthenticationChanged += OnAuthenticationChanged;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var version = ++_loadVersion;
        _loading = true;

        try
        {
            var result = await Api.SendAsync<PlatformStaffPageResponse>(
                HttpMethod.Get,
                $"api/v1/control-plane/staff?page={_page}&search={Uri.EscapeDataString(_search)}",
                cancellationToken: _lifetime.Token
            );

            if (_disposed || version != _loadVersion)
            {
                return;
            }

            _items = result.IsSuccess ? result.Data?.Items ?? [] : [];
            _total = result.Data?.Total ?? 0;

            if (!result.IsSuccess)
            {
                _message = UserMessageSanitizer.Normalize(result.Message, "Staff could not be loaded.");
            }
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        finally
        {
            if (!_disposed && version == _loadVersion)
            {
                _loading = false;
            }
        }
    }

    private async Task SearchAsync(string value)
    {
        _search = value;
        _page = 1;
        await LoadAsync();
    }

    private async Task PageAsync(int value)
    {
        _page = value;
        await LoadAsync();
    }

    private string[] InvitationActions(PlatformStaffResponse item)
    {
        return AuthSession.HasPermission(PlatformPermissions.ManagePlatformStaff) && item.IsStaff && item.IsActive && !item.EmailConfirmed ? ["Send Invitation"] : [];
    }

    private async Task EditAsync(PlatformStaffResponse? item)
    {
        var dialog = await Dialogs.ShowAsync<PlatformStaffDialog>(
            item is null ? "Add Platform Staff" : "Edit Platform Staff",
            new DialogParameters<PlatformStaffDialog> { { d => d.Account, item } },
            new DialogOptions { FullWidth = true, MaxWidth = MaxWidth.Medium }
        );

        _openDialog = dialog;
        var result = await dialog.Result;
        _openDialog = null;

        if (!_disposed && result is { Canceled: false })
        {
            await LoadAsync();
        }
    }

    private async Task InviteAsync(PlatformStaffResponse item)
    {
        var dialog = await Dialogs.ShowAsync<PlatformStaffInvitationDialog>(
            "Send Staff Invitation",
            new DialogParameters<PlatformStaffInvitationDialog> { { d => d.Account, item } },
            new DialogOptions { FullWidth = true, MaxWidth = MaxWidth.Small }
        );

        _openDialog = dialog;
        var result = await dialog.Result;
        _openDialog = null;

        if (!_disposed && result is { Canceled: false })
        {
            _message = "Invitation sent. The account remains restricted until email verification and MFA setup.";
            await LoadAsync();
        }
    }

    private async Task HistoryAsync(PlatformStaffResponse item)
    {
        _openDialog = await Dialogs.ShowAsync<PlatformStaffHistoryDialog>(
            "Staff Access History",
            new DialogParameters<PlatformStaffHistoryDialog> { { d => d.Account, item } },
            new DialogOptions { FullWidth = true, MaxWidth = MaxWidth.Large }
        );
    }

    private void OnAuthenticationChanged()
    {
        _ = InvokeAsync(async () =>
        {
            if (_disposed)
            {
                return;
            }

            var authority = AuthorityFingerprint();

            if (string.Equals(_authority, authority, StringComparison.Ordinal))
            {
                return;
            }

            _authority = authority;
            ++_loadVersion;
            _items = [];
            _total = 0;
            _message = null;
            _openDialog?.Close(DialogResult.Cancel());
            _openDialog = null;

            if (AuthSession.HasPermission(PlatformPermissions.ViewPlatformStaff))
            {
                await LoadAsync();
            }

            StateHasChanged();
        });
    }

    private string AuthorityFingerprint()
    {
        var user = AuthSession.CurrentUser;

        if (!AuthSession.IsAuthenticated || user is null)
        {
            return string.Empty;
        }

        return string.Join(
            "|",
            user.AppUserId,
            user.TenantId,
            user.SessionId,
            user.IsPlatformStaff,
            user.HasMfaProof,
            user.MfaEnrollmentRequired,
            user.AdministratorSetupRequired,
            user.SupportGrantId,
            user.IsReadOnlySupport,
            user.IsReadOnlyDemo,
            string.Join(",", (user.Permissions ?? []).Order(StringComparer.Ordinal)),
            string.Join(",", user.UnavailablePermissions.Order(StringComparer.Ordinal)));
    }

    public void Dispose()
    {
        _disposed = true;
        ++_loadVersion;
        AuthSession.AuthenticationChanged -= OnAuthenticationChanged;
        _openDialog?.Close(DialogResult.Cancel());
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
