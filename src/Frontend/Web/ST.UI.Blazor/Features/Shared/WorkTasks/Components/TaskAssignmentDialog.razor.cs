using Microsoft.AspNetCore.Components;
using MudBlazor;
using ST.SharedKernel.Features.Shared.Dashboard;
using ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;
using ST.UI.Blazor.Features.Shared.Messaging;

namespace ST.UI.Blazor.Features.Shared.WorkTasks.Components;

public partial class TaskAssignmentDialog : IDisposable
{
    [Inject] private IBackendApiClient Api { get; set; } = null!;
    [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = null!;
    [Parameter, EditorRequired] public DashboardTaskResponse Item { get; set; } = null!;
    private readonly CancellationTokenSource _lifetime = new();
    private DashboardTaskAssigneeResponse? _selected;
    private string? _message;
    private bool _busy;
    private bool _disposed;

    private async Task<IEnumerable<DashboardTaskAssigneeResponse>> SearchAsync(string? text, CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, cancellationToken);

        try
        {
            var response = await Api.SendAsync<IReadOnlyList<DashboardTaskAssigneeResponse>>(HttpMethod.Get,
                TaskApiRoutes.ForTask(Item.Task, "assignees") + $"&search={Uri.EscapeDataString(text ?? string.Empty)}", cancellationToken: linked.Token);

            if (_disposed) return [];
            _message = response.IsSuccess ? null : UserMessageSanitizer.Normalize(response.Message, "Eligible accounts could not be loaded.");
            await InvokeAsync(StateHasChanged);
            return response.Data ?? [];
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested) { return []; }
    }

    private async Task SaveAsync()
    {
        if (_selected is null) return;
        _busy = true;

        try
        {
            var response = await Api.SendAsync<bool>(HttpMethod.Post, $"api/v1/dashboard/tasks/{Item.Task.Kind}/assignment",
                new AssignDashboardTaskRequest(Item.Task, _selected.UserId, Item.Revision), cancellationToken: _lifetime.Token);

            if (_disposed) return;

            if (response.IsSuccess) Dialog.Close(DialogResult.Ok(true));
            else _message = UserMessageSanitizer.Normalize(response.Message, "Assignment could not be updated. Refresh the queue and try again.");
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        finally { _busy = false; }
    }

    private void Cancel()
    {
        Dialog.Cancel();
    }

    public void Dispose()
    {
        _disposed = true;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
