using Microsoft.AspNetCore.Components;
using MudBlazor;
using ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;
using ST.UI.Blazor.Features.Shared.Messaging;
namespace ST.UI.Blazor.Features.ControlPlane.Tenants.Pages;

public partial class KycRetention
{
    [SupplyParameterFromQuery] public Guid? TenantId { get; set; }
    private IReadOnlyList<KycRetentionPolicyResponse> _policies = [];
    private KycRetentionStatusResponse? _case;
    private string _name = string.Empty, _policyReason = string.Empty, _caseReason = string.Empty;
    private string? _message;
    private int _months = 24, _closed = 90, _inactive = 30, _recovery = 30, _orphans = 7, _holdReview = 90;
    private bool _busy;
    private Severity _severity;
    private KycRetentionPolicyResponse? AssignedPolicy => _policies.FirstOrDefault(item => item.Id == _case?.PolicyId);

    private static string ScheduleSummary(KycRetentionPolicyResponse policy)
    {
        return $"After relationship end: {policy.ApprovedMonths} months; closure: {policy.ClosedDays} days; inactivity: {policy.InactiveDays} days; recovery: {policy.RecoveryDays} days; unattached uploads: {policy.OrphanDays} days; hold review: {policy.HoldReviewDays} days.";
    }

    private const string Root = "api/v1/control-plane/kyc/retention";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) { await LoadAsync(); StateHasChanged(); }
    }

    private async Task LoadAsync()
    {
        var response = await Api.SendAsync<IReadOnlyList<KycRetentionPolicyResponse>>(HttpMethod.Get, $"{Root}/policies");
        if (!response.IsSuccess) { Show(response.Message, false); return; }
        _policies = response.Data ?? [];
        if (_policies.Count > 0)
        {
            var p = _policies[0]; _name = p.Name; _months = p.ApprovedMonths; _closed = p.ClosedDays;
            _inactive = p.InactiveDays; _recovery = p.RecoveryDays; _orphans = p.OrphanDays; _holdReview = p.HoldReviewDays;
        }
        if (TenantId.HasValue)
        {
            var status = await Api.SendAsync<KycRetentionStatusResponse>(HttpMethod.Get, $"{Root}/cases/{TenantId}");
            if (status.IsSuccess) _case = status.Data; else Show(status.Message, false);
        }
    }

    private async Task PublishAsync()
    {
        _busy = true;
        try
        {
            var response = await Api.SendAsync<Guid>(HttpMethod.Post, $"{Root}/policies", new PublishKycRetentionPolicyRequest(
                _policies[0].Id, _name, _months, _closed, _inactive, _recovery, _orphans, _holdReview, _policyReason));
            Show(response.IsSuccess ? "New policy published. Existing case schedules are unchanged." : response.Message, response.IsSuccess);
            if (response.IsSuccess) { _policyReason = string.Empty; await LoadAsync(); }
        }
        finally { _busy = false; }
    }

    private Task ChangeHoldAsync() { return ActAsync(_case?.Held == true ? "release" : "hold"); }

    private async Task ActAsync(string action)
    {
        if (_case is null) return;
        _busy = true;
        try
        {
            var suffix = action is "hold" or "release" ? "/hold" : string.Empty;
            var response = await Api.SendAsync<bool>(HttpMethod.Post, $"{Root}/cases/{TenantId}{suffix}",
                new KycRetentionActionRequest(_case.Revision, action, _caseReason));
            Show(response.IsSuccess ? "Case retention updated." : response.Message, response.IsSuccess);
            await LoadAsync();
            if (response.IsSuccess) _caseReason = string.Empty;
        }
        finally { _busy = false; }
    }

    private void Show(string? message, bool success)
    {
        _message = UserMessageSanitizer.Normalize(message, success ? "Updated." : "The request could not be completed.");
        _severity = success ? Severity.Success : Severity.Error;
    }
}
