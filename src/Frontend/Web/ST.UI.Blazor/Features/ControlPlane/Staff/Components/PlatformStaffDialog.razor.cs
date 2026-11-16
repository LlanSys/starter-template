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

namespace ST.UI.Blazor.Features.ControlPlane.Staff.Components;

public partial class PlatformStaffDialog
{
    [Inject]
    private IBackendApiClient Api { get; set; } = default!;

    [Inject]
    private IAuthSession AuthSession { get; set; } = default!;

    [CascadingParameter]
    private IMudDialogInstance Dialog { get; set; } = default!;

    [Parameter]
    public PlatformStaffResponse? Account { get; set; }

    private readonly HashSet<string> _keys = new(StringComparer.OrdinalIgnoreCase);
    private string _first = string.Empty, _last = string.Empty, _username = string.Empty, _email = string.Empty, _reason = string.Empty, _profile = string.Empty;
    private bool _staff = true, _active = true, _saving;
    private string? _error;
    protected override void OnInitialized()
    {
        if (Account is not null)
        {
            _staff = Account.IsStaff;
            _active = Account.IsActive;
            _keys.UnionWith(Account.PermissionKeys);
        }
    }

    private void ApplyProfile(string key)
    {
        _profile = key;
        _keys.Clear();
        _keys.UnionWith(PlatformStaffProfiles.All.Single(p => p.Key == key).PermissionKeys);
    }

    private void Toggle(string key, bool value)
    {
        if (value)
        {
            _keys.Add(key);
        }
        else
        {
            _keys.Remove(key);
        }

        _profile = string.Empty;
    }

    private static string Label(string key)
    {
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(key.Replace(
            "control_plane.",
            string.Empty,
            StringComparison.Ordinal
        ).Replace('_', ' '));
    }

    private void Cancel()
    {
        Dialog.Cancel();
    }

    private async Task SaveAsync()
    {
        _saving = true;

        try
        {
            var result = Account is null ? await Api.SendAsync<PlatformStaffResponse>(
                HttpMethod.Post,
                "api/v1/control-plane/staff",
                new CreatePlatformStaffRequest(
                    _username.Trim(),
                    _email.Trim(),
                    _first.Trim(),
                    _last.Trim(),
                    _keys.ToArray(),
                    _reason.Trim()
                )
            ) : await Api.SendAsync<PlatformStaffResponse>(
                HttpMethod.Put,
                $"api/v1/control-plane/staff/{Account.UserId}",
                new UpdatePlatformStaffRequest(Account.Revision, _staff, _active, _keys.ToArray(), _reason.Trim())
            );

            if (result.IsSuccess)
            {
                Dialog.Close(DialogResult.Ok(result.Data));
            }
            else
            {
                _error = UserMessageSanitizer.Normalize(result.Message, "The staff account could not be saved. Check the required fields.");
            }
        }
        finally
        {
            _saving = false;
        }
    }
}
