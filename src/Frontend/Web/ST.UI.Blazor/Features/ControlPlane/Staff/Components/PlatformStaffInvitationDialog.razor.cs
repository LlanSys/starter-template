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

public partial class PlatformStaffInvitationDialog
{
    [Inject]
    private IBackendApiClient Api { get; set; } = default!;

    [CascadingParameter]
    private IMudDialogInstance Dialog { get; set; } = default!;

    [Parameter, EditorRequired]
    public PlatformStaffResponse Account { get; set; } = default!;

    private string _reason = string.Empty;
    private string? _error;
    private bool _busy;
    private void Cancel()
    {
        Dialog.Cancel();
    }

    private async Task SendAsync()
    {
        _busy = true;

        try
        {
            var result = await Api.SendAsync<bool>(
                HttpMethod.Post,
                $"api/v1/control-plane/staff/{Account.UserId}/invitation",
                new PlatformStaffInvitationRequest(Account.Revision, _reason.Trim())
            );

            if (result.IsSuccess)
            {
                Dialog.Close(DialogResult.Ok(true));
            }
            else
            {
                _error = UserMessageSanitizer.Normalize(result.Message, "Invitation could not be sent. Refresh the list before retrying.");
            }
        }
        finally
        {
            _busy = false;
        }
    }
}
