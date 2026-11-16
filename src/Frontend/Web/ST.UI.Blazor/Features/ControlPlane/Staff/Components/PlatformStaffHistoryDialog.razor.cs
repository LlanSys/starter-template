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

public partial class PlatformStaffHistoryDialog
{
    [Inject]
    private IBackendApiClient Api { get; set; } = default!;

    [CascadingParameter]
    private IMudDialogInstance Dialog { get; set; } = default!;

    [Parameter, EditorRequired]
    public PlatformStaffResponse Account { get; set; } = default!;

    private IReadOnlyList<PlatformStaffAuditResponse> _records = [];
    private bool _loading = true;
    private string? _message;
    protected override async Task OnInitializedAsync()
    {
        var result = await Api.SendAsync<IReadOnlyList<PlatformStaffAuditResponse>>(HttpMethod.Get, $"api/v1/control-plane/staff/{Account.UserId}/history");
        _records = result.Data ?? [];

        if (!result.IsSuccess)
        {
            _message = UserMessageSanitizer.Normalize(result.Message, "Staff history could not be loaded.");
        }

        _loading = false;
    }

    private void Close()
    {
        Dialog.Close();
    }
}
