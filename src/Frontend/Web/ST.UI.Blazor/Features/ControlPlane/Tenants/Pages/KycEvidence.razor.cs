using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;
using ST.UI.Blazor.Features.Shared.Messaging;

namespace ST.UI.Blazor.Features.ControlPlane.Tenants.Pages;

public partial class KycEvidence
{
    [Inject] private IBackendApiClient Api { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter]
    public Guid TenantId
    {
        get; set;
    }

    private KycCaseSummaryResponse? _tenant;
    private IReadOnlyList<KycDocumentResponse> _documents = [];
    private KycDocumentResponse? _reviewDocument;
    private string _documentFilter = string.Empty;
    private IEnumerable<KycDocumentResponse> FilteredDocuments => _documents.Where(document =>
        string.IsNullOrEmpty(_documentFilter) || document.DocumentTypeKey == _documentFilter);
    private string _feedback = string.Empty;
    private string? _message;
    private string? _link;
    private DateTimeOffset? _expiresAt;
    private Severity _severity = Severity.Info;
    private bool _busy;
    private bool _approvalConfirmed;
    private string _approvalReference = string.Empty;
    private IReadOnlyList<KycSubmittedDocumentResponse> UploadHistory => _documents.Select(document =>
        new KycSubmittedDocumentResponse(document.DocumentType, document.FileName, document.UploadedAt, document.Status,
            document.Reviews.FirstOrDefault()?.Feedback)
        {
            DocumentTypeKey = document.DocumentTypeKey
        }).ToArray();
    private string Root => $"api/v1/control-plane/tenants/{TenantId}/kyc";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadAsync();
            StateHasChanged();
        }
    }

    private async Task LoadAsync()
    {
        var tenant = await Api.SendAsync<KycCaseSummaryResponse>(HttpMethod.Get, $"{Root}/case");

        if (!tenant.IsSuccess)
        {
            Show(tenant.Message, false);
            return;
        }

        _tenant = tenant.Data;
        var documents = await Api.SendAsync<IReadOnlyList<KycDocumentResponse>>(HttpMethod.Get, $"{Root}/documents");

        if (documents.IsSuccess)
        {
            _documents = documents.Data ?? [];
        }
        else
        {
            Show(documents.Message, false);
        }
    }

    private async Task ApproveKycAsync()
    {
        if (!_approvalConfirmed || string.IsNullOrWhiteSpace(_approvalReference))
        {
            return;
        }

        _busy = true;

        try
        {
            var result = await Api.SendAsync<TenantResponse>(HttpMethod.Post,
                $"api/v1/control-plane/tenants/{TenantId}/approve", new ApproveTenantRequest(_approvalReference));

            Show(result.IsSuccess ? "KYC approved. Onboarding can continue." : result.Message, result.IsSuccess);

            if (result.IsSuccess)
            {
                await LoadAsync();
            }
        }
        finally { _busy = false; }
    }

    private async Task IssueLinkAsync()
    {
        _busy = true;

        try
        {
            var result = await Api.SendAsync<KycSubmissionLinkResponse>(HttpMethod.Post, $"{Root}/submission-link");

            if (result.IsSuccess && result.Data is not null)
            {
                _link = $"{Navigation.BaseUri}onboarding/kyc/{TenantId}#token={result.Data.Token}";
                _expiresAt = result.Data.ExpiresAt;
                Show("The submission link is ready to share.", true);
            }
            else
            {
                Show(result.Message, false);
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task RevokeLinkAsync()
    {
        _busy = true;

        try
        {
            var result = await Api.SendAsync<bool>(HttpMethod.Delete, $"{Root}/submission-link");

            if (result.IsSuccess)
            {
                _link = null;
            }

            Show(result.IsSuccess ? "The submission link is closed." : result.Message, result.IsSuccess);
        }
        finally
        {
            _busy = false;
        }
    }

    private void SelectReview(KycDocumentResponse document)
    {
        _reviewDocument = document;
        _feedback = string.Empty;
    }

    private async Task ReviewAsync(bool accepted)
    {
        if (_reviewDocument is null)
        {
            return;
        }

        _busy = true;

        try
        {
            var result = await Api.SendAsync<bool>(HttpMethod.Post, $"{Root}/documents/{_reviewDocument.Id}/reviews",
                new ReviewKycDocumentRequest(accepted, _feedback));

            Show(result.IsSuccess ? "Document review saved." : result.Message, result.IsSuccess);

            if (result.IsSuccess)
            {
                _reviewDocument = null;
                await LoadAsync();
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task RetrySafetyAsync(KycDocumentResponse document)
    {
        _busy = true;

        try
        {
            var result = await Api.SendAsync<bool>(HttpMethod.Post, $"{Root}/documents/{document.Id}/safety-retry",
                new RetryKycSafetyScanRequest(document.Revision));

            Show(result.IsSuccess ? "Safety check queued." : result.Message, result.IsSuccess);
            await LoadAsync();
        }
        finally { _busy = false; }
    }

    private async Task DownloadAsync(KycDocumentResponse document)
    {
        _busy = true;

        try
        {
            var result = await Api.SendFileAsync($"{Root}/documents/{document.Id}/content");

            if (!result.IsSuccess || result.Data is null)
            {
                Show(result.Message, false);
                await LoadAsync();
                return;
            }

            await using var module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/documentDownload.js");
            using var stream = new MemoryStream(result.Data.Content, writable: false);
            using var reference = new DotNetStreamReference(stream);
            await module.InvokeVoidAsync("download", document.FileName, result.Data.ContentType, reference);
        }
        catch (JSException)
        {
            Show("The download could not start. Please try again.", false);
        }
        finally
        {
            _busy = false;
        }
    }

    private void Show(string? message, bool success)
    {
        _message = UserMessageSanitizer.Normalize(message, success ? "Updated." : "The request could not be completed.");
        _severity = success ? Severity.Success : Severity.Error;
    }
}
