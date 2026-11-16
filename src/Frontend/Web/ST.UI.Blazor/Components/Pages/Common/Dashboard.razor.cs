using System.Globalization;
using Microsoft.AspNetCore.Components;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using ST.SharedKernel.Features.Shared.Dashboard;
using ST.UI.Blazor.Features.ControlPlane.Stamps.Contracts;
using ST.UI.Blazor.Features.IAM.Users.Contracts.Interfaces;
using ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;
using ST.UI.Blazor.Features.Shared.Messaging;
using ST.UI.Blazor.Logging;

namespace ST.UI.Blazor.Components.Pages.Common;

public partial class Dashboard : IDisposable
{
    [Inject] private IAuthSession AuthSession { get; set; } = null!;
    [Inject] private IBackendApiClient Api { get; set; } = null!;
    [Inject] private IDeploymentStampService Stamps { get; set; } = null!;
    [Inject] private ILogger<Dashboard> Logger { get; set; } = null!;

    private bool _loading = true;
    private bool _started;
    private bool _disposed;
    private int _version;
    private string? _message;
    private string? _attentionMessage;
    private string _accessSnapshot = string.Empty;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _activeLoad;
    private readonly List<(string Label, string Value, string Accent)> _metrics = [];
    private readonly List<DashboardAttentionItemResponse> _attention = [];
    private IEnumerable<DashboardAttentionItemResponse> VisibleAttention => _attention
        .Where(item => item.RequiredPermissions.All(AuthSession.HasPermission));

    private DashboardAttentionItemResponse[] AssignedActions => VisibleAttention
        .Where(item => item.Kind == DashboardAttentionKind.AssignedAction).ToArray();
    private DashboardAttentionItemResponse[] PersonalActions => VisibleAttention
        .Where(item => item.Kind == DashboardAttentionKind.PersonalAction).ToArray();
    private DashboardAttentionItemResponse[] TeamActions => VisibleAttention
        .Where(item => item.Kind == DashboardAttentionKind.TeamAction).ToArray();
    private DashboardAttentionItemResponse[] Information => VisibleAttention
        .Where(item => item.Kind == DashboardAttentionKind.Information).ToArray();

    protected override void OnInitialized()
    {
        AuthSession.AuthenticationChanged += OnAccessChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }
        await AuthSession.InitializeAsync();
        _started = true;
        _accessSnapshot = AccessSnapshot();
        await ReloadAsync();
    }

    private void OnAccessChanged()
    {
        if (!_started || _disposed)
        {
            return;
        }
        _ = InvokeAsync(RefreshAccessAsync);
    }

    private async Task RefreshAccessAsync()
    {
        var snapshot = AccessSnapshot();

        if (_disposed || snapshot == _accessSnapshot)
        {
            return;
        }
        _accessSnapshot = snapshot;
        _version++;
        _activeLoad?.Cancel();
        _attention.Clear();
        _metrics.Clear();
        await ReloadAsync();
    }

    private string AccessSnapshot()
    {
        var user = AuthSession.CurrentUser;
        return user is null ? string.Empty : string.Join("|", user.AppUserId, user.TenantId,
            user.IsPlatformStaff, user.HasMfaProof, user.TwoFactorEnabled,
            string.Join(',', (user.Permissions ?? []).Order(StringComparer.OrdinalIgnoreCase)),
            string.Join(',', user.UnavailablePermissions.Order(StringComparer.OrdinalIgnoreCase)));
    }

    private async Task ReloadAsync()
    {
        if (_disposed)
        {
            return;
        }
        var version = ++_version;
        _activeLoad?.Cancel();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _activeLoad = cancellation;
        _loading = true;
        _message = null;
        _attentionMessage = null;
        StateHasChanged();
        var user = AuthSession.CurrentUser;

        try
        {
            var attentionTask = Api.SendAsync<IReadOnlyList<DashboardAttentionItemResponse>>(
                HttpMethod.Get, "api/v1/dashboard/attention", cancellationToken: cancellation.Token);

            var metricsTask = LoadPlatformMetricsAsync(cancellation.Token);
            await Task.WhenAll(attentionTask, metricsTask);

            if (_disposed || version != _version)
            {
                return;
            }
            var attention = await attentionTask;
            var metrics = await metricsTask;
            _metrics.Clear();
            _metrics.AddRange(metrics.Items);
            _message = metrics.Message;
            _attention.Clear();

            if (attention.IsSuccess && attention.Data is not null)
            {
                _attention.AddRange(attention.Data);
            }
            else
            {
                _attentionMessage = UserMessageSanitizer.Normalize(attention.Message, "Attention items could not be loaded.");
            }

            if (user is { TwoFactorEnabled: false })
            {
                _attention.Add(new DashboardAttentionItemResponse("account-mfa", "Set up your authenticator", 1,
                    "/iam/security", [], DashboardAttentionKind.PersonalAction,
                    "Your account does not have an authenticator enrolled.", "Set up account security"));
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // Navigation or an authority change superseded this snapshot.
        }
        catch (Exception exception)
        {
            DashboardLogDefinitions.LoadFailed(Logger, exception);

            if (!_disposed && version == _version)
            {
                _attention.Clear();
                _attentionMessage = "Dashboard items could not be loaded. Please try again.";
            }
        }
        finally
        {
            if (ReferenceEquals(_activeLoad, cancellation))
            {
                _activeLoad = null;
            }

            if (!_disposed && version == _version)
            {
                _loading = false;
                StateHasChanged();
            }
        }
    }

    private async Task<(List<(string Label, string Value, string Accent)> Items, string? Message)> LoadPlatformMetricsAsync(CancellationToken cancellationToken)
    {
        var items = new List<(string Label, string Value, string Accent)>();
        string? message = null;

        if (AuthSession.CurrentUser?.IsPlatformStaff != true)
        {
            return (items, message);
        }

        if (AuthSession.HasPermission(PlatformPermissions.ViewTenants))
        {
            var response = await Api.SendAsync<PlatformDashboardResponse>(
                HttpMethod.Get, "api/v1/control-plane/tenants/dashboard", cancellationToken: cancellationToken);

            if (response.IsSuccess && response.Data is { } metrics)
            {
                AddMetric("Customer tenants", metrics.CustomerTenants, "primary");
                AddMetric("Active tenants", metrics.ActiveTenants, "success");
                AddMetric("Suspended tenants", metrics.SuspendedTenants, "neutral");
                AddMetric("Awaiting KYC", metrics.AwaitingKyc, "warning");
                AddMetric("Awaiting provisioning", metrics.PendingProvisioning, "info");
                AddMetric("Provisioning", metrics.Provisioning, "info");
                AddMetric("Provisioning failures", metrics.ProvisioningFailed, "danger");

                if (metrics.InvitationsPending is { } pending)
                {
                    AddMetric("Invitations pending", pending, "warning");
                }

                if (metrics.InvitationDeliveryRetries is { } retries)
                {
                    AddMetric("Invitation delivery retries", retries, "danger");
                }
            }
            else
            {
                message = UserMessageSanitizer.Normalize(response.Message, "Tenant metrics could not be loaded.");
            }
        }

        if (AuthSession.HasPermission(PlatformPermissions.ViewStamps))
        {
            var response = await Stamps.GetAllAsync(cancellationToken);

            if (response.IsSuccess && response.Data is { } stamps)
            {
                AddMetric("Deployment stamps", stamps.Count, "primary");
                AddMetric("Pooled stamps", stamps.Count(stamp => stamp.IsolationTier.Equals("Pooled", StringComparison.OrdinalIgnoreCase)), "success");
                AddMetric("Isolated stamps", stamps.Count(stamp => stamp.IsolationTier.Equals("Isolated", StringComparison.OrdinalIgnoreCase)), "neutral");
            }
            else
            {
                message = UserMessageSanitizer.Normalize(response.Message, "Stamp metrics could not be loaded.");
            }
        }
        return (items, message);

        void AddMetric(string label, int value, string accent)
        {
            items.Add((label, value.ToString(CultureInfo.InvariantCulture), accent));
        }
    }

    public void Dispose()
    {
        _disposed = true;
        AuthSession.AuthenticationChanged -= OnAccessChanged;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
