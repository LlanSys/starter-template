using Microsoft.AspNetCore.Components;
using MudBlazor;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.Shared.Dashboard;
using ST.UI.Blazor.Features.IAM.Users.Contracts.Interfaces;
using ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;
using ST.UI.Blazor.Features.Shared.Messaging;
using ST.UI.Blazor.Features.Shared.WorkTasks.Components;

namespace ST.UI.Blazor.Features.Shared.WorkTasks.Pages;

public partial class WorkTasks : IDisposable
{
    [Inject] private IAuthSession AuthSession { get; set; } = null!;
    [Inject] private IBackendApiClient Api { get; set; } = null!;
    [Inject] private IDialogService Dialogs { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Parameter, SupplyParameterFromQuery(Name = "kind")] public string? RequestedKind { get; set; }
    [Parameter, SupplyParameterFromQuery(Name = "queue")] public string? RequestedQueue { get; set; }
    private const int PageSize = 25;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _activeLoad;
    private IDialogReference? _openDialog;
    private IReadOnlyList<DashboardTaskResponse> _items = [];
    private bool _ready;
    private bool _loading = true;
    private bool _busy;
    private bool _disposed;
    private bool _canAssignOthers;
    private int _page = 1;
    private int _total;
    private int _version;
    private int _tab;
    private string _kind = string.Empty;
    private string _snapshot = string.Empty;
    private string? _message;
    private Severity _severity = Severity.Warning;
    private DashboardTaskDefinition? Definition => DashboardTaskCatalog.Definitions.GetValueOrDefault(_kind);
    private DashboardTaskDefinition[] AllowedDefinitions => AuthSession.CurrentUser is { } user && user.SupportGrantId is null && !user.IsDemoWorkspace
        ? DashboardTaskCatalog.Definitions.Values.Where(definition => definition.RequiredPermissions.All(AuthSession.HasPermission)
            && (!definition.IsPlatform || user.IsPlatformStaff && user.HasMfaProof)).OrderBy(definition => definition.Title).ToArray() : [];
    private bool CanManageCategory => Definition is { } definition && AuthSession.HasPermission(definition.IsPlatform
        ? PlatformPermissions.ManageTasks : DashboardTaskCatalog.CustomerAssignmentPermission);
    private string Queue => _tab == 0 ? "mine" : _tab == 1 ? "unassigned" : "all";

    protected override void OnInitialized()
    {
        AuthSession.AuthenticationChanged += OnAuthenticationChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await AuthSession.InitializeAsync();
        _ready = true;
        _snapshot = Snapshot();
        SelectRequestedQueue();
        await ReloadAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!_ready) return;
        SelectRequestedQueue();
        _page = 1;
        await ReloadAsync();
    }

    private void SelectRequestedQueue()
    {
        _kind = AllowedDefinitions.FirstOrDefault(item => item.Kind == RequestedKind)?.Kind ?? AllowedDefinitions.FirstOrDefault()?.Kind ?? string.Empty;
        _tab = RequestedQueue == "unassigned" ? 1 : RequestedQueue == "all" && CanManageCategory ? 2 : 0;
    }

    private Task ChangeKindAsync(string value)
    {
        Navigation.NavigateTo($"/tasks?kind={Uri.EscapeDataString(value)}&queue={Queue}");
        return Task.CompletedTask;
    }

    private Task ChangeTabAsync(int value)
    {
        var queue = value == 0 ? "mine" : value == 1 ? "unassigned" : "all";
        Navigation.NavigateTo($"/tasks?kind={Uri.EscapeDataString(_kind)}&queue={queue}");
        return Task.CompletedTask;
    }

    private async Task ChangePageAsync(int value)
    {
        _page = value;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (_disposed) return;
        var version = ++_version;
        _activeLoad?.Cancel();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _activeLoad = cancellation;
        _loading = true;
        _message = null;
        _canAssignOthers = false;
        StateHasChanged();

        try
        {
            if (!AllowedDefinitions.Any(item => item.Kind == _kind))
            {
                _items = [];
                _total = 0;
                return;
            }
            var response = await Api.SendAsync<DashboardTaskPageResponse>(HttpMethod.Get,
                $"api/v1/dashboard/tasks/{Uri.EscapeDataString(_kind)}?queue={Queue}&page={_page}&pageSize={PageSize}", cancellationToken: cancellation.Token);

            if (_disposed || version != _version) return;
            _items = response.IsSuccess ? response.Data?.Items ?? [] : [];
            _total = response.IsSuccess ? response.Data?.Total ?? 0 : 0;
            _canAssignOthers = response.IsSuccess && response.Data?.CanAssignOthers == true;

            if (!response.IsSuccess) _message = UserMessageSanitizer.Normalize(response.Message, "Tasks could not be loaded.");
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        finally
        {
            if (ReferenceEquals(_activeLoad, cancellation)) _activeLoad = null;

            if (!_disposed && version == _version) { _loading = false; StateHasChanged(); }
        }
    }

    private async Task UpdateAsync(DashboardTaskResponse item, string? assignee)
    {
        _busy = true;
        var snapshot = _snapshot;

        try
        {
            var response = await Api.SendAsync<bool>(HttpMethod.Post, $"api/v1/dashboard/tasks/{item.Task.Kind}/assignment",
                new AssignDashboardTaskRequest(item.Task, assignee, item.Revision), cancellationToken: _lifetime.Token);

            if (_disposed || snapshot != _snapshot) return;
            await ReloadAsync();
            _severity = response.IsSuccess ? Severity.Success : Severity.Warning;
            _message = UserMessageSanitizer.Normalize(response.Message, response.IsSuccess ? "Assignment updated." : "Assignment could not be updated.");
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        finally { _busy = false; }
    }

    private async Task AssignAsync(DashboardTaskResponse item)
    {
        var parameters = new DialogParameters<TaskAssignmentDialog> { { dialog => dialog.Item, item } };
        _openDialog = await Dialogs.ShowAsync<TaskAssignmentDialog>("Assign Task", parameters,
            new DialogOptions { FullWidth = true, MaxWidth = MaxWidth.Small });

        var snapshot = _snapshot;
        var result = await _openDialog.Result;
        _openDialog = null;

        if (!_disposed && snapshot == _snapshot && result is { Canceled: false }) await ReloadAsync();
    }

    private async Task HistoryAsync(DashboardTaskResponse item)
    {
        var parameters = new DialogParameters<TaskHistoryDialog> { { dialog => dialog.Task, item.Task } };
        _openDialog = await Dialogs.ShowAsync<TaskHistoryDialog>("Recent Assignment History", parameters,
            new DialogOptions { FullWidth = true, MaxWidth = MaxWidth.Medium });

        await _openDialog.Result;
        _openDialog = null;
    }

    private string Snapshot()
    {
        var user = AuthSession.CurrentUser;
        return user is null ? string.Empty : string.Join('|', user.AppUserId, user.TenantId, user.IsPlatformStaff, user.HasMfaProof, user.TwoFactorEnabled, user.SupportGrantId, user.IsDemoWorkspace,
            string.Join(',', (user.Permissions ?? []).Order(StringComparer.Ordinal)), string.Join(',', user.UnavailablePermissions.Order(StringComparer.Ordinal)));
    }

    private void OnAuthenticationChanged()
    {
        if (_ready && !_disposed) _ = InvokeAsync(RefreshAccessAsync);
    }

    private async Task RefreshAccessAsync()
    {
        var snapshot = Snapshot();

        if (_disposed || snapshot == _snapshot) return;
        _snapshot = snapshot;
        ++_version;
        _activeLoad?.Cancel();
        _openDialog?.Close(DialogResult.Cancel());
        _items = [];
        _total = 0;
        SelectRequestedQueue();
        _page = 1;
        await ReloadAsync();
    }

    public void Dispose()
    {
        _disposed = true;
        AuthSession.AuthenticationChanged -= OnAuthenticationChanged;
        _openDialog?.Close(DialogResult.Cancel());
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
