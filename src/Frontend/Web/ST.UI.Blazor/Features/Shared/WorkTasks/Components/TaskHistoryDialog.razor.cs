using Microsoft.AspNetCore.Components;
using MudBlazor;
using ST.SharedKernel.Features.Shared.Dashboard;
using ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;
using ST.UI.Blazor.Features.Shared.Messaging;

namespace ST.UI.Blazor.Features.Shared.WorkTasks.Components;

public partial class TaskHistoryDialog : IDisposable
{
    [Inject] private IBackendApiClient Api { get; set; } = null!;
    [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = null!;
    [Parameter, EditorRequired] public DashboardTaskKey Task { get; set; } = null!;
    private readonly CancellationTokenSource _lifetime = new();
    private IReadOnlyList<DashboardTaskHistoryResponse> _history = [];
    private bool _loading = true;
    private bool _disposed;
    private string? _message;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var response = await Api.SendAsync<IReadOnlyList<DashboardTaskHistoryResponse>>(HttpMethod.Get,
                TaskApiRoutes.ForTask(Task, "history"), cancellationToken: _lifetime.Token);

            if (_disposed) return;
            _history = response.Data ?? [];

            if (!response.IsSuccess) _message = UserMessageSanitizer.Normalize(response.Message, "Assignment history could not be loaded.");
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        finally { _loading = false; }
    }

    private void Close()
    {
        Dialog.Close();
    }

    public void Dispose()
    {
        _disposed = true;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
