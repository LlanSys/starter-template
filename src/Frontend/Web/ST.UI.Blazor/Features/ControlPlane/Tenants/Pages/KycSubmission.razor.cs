using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;
using ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;
using ST.UI.Blazor.Features.Shared.Messaging;

namespace ST.UI.Blazor.Features.ControlPlane.Tenants.Pages;

public partial class KycSubmission
{
    [Inject] private IBackendApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter] public Guid TenantId { get; set; }

    private string _token = string.Empty;
    private KycSubmissionStatusResponse? _status;
    private string? _message;
    private Severity _severity = Severity.Info;
    private bool _busy;
    private string Root => $"api/v1/onboarding/kyc/{TenantId}";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var fragment = new Uri(Navigation.Uri).Fragment.TrimStart('#');
            _token = QueryHelpers.ParseQuery(fragment).TryGetValue("token", out var token) ? token.ToString() : string.Empty;
            await InspectAsync();
            StateHasChanged();
        }
    }

    private async Task InspectAsync()
    {
        _busy = true;
        try
        {
            var result = await Api.SendAsync<KycSubmissionStatusResponse>(HttpMethod.Post, $"{Root}/inspect",
                new InspectKycSubmissionRequest(_token), requiresAuthentication: false, includeWorkspaceSelector: false);
            _status = result.IsSuccess ? result.Data : null;
            if (!result.IsSuccess)
            {
                _message = UserMessageSanitizer.Normalize(result.Message, "This submission link is unavailable. Contact your onboarding team.");
                _severity = Severity.Error;
            }
        }
        finally
        {
            _busy = false;
        }
    }

}
