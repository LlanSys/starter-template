using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using ST.SharedKernel.Features.ControlPlane.Kyc;
using ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;
using ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;
using ST.UI.Blazor.Features.Shared.Messaging;
using System.Net.Http.Headers;

namespace ST.UI.Blazor.Features.ControlPlane.Tenants.Components;

public partial class KycDocumentUploads
{
    [Inject] private IBackendApiClient Api { get; set; } = default!;
    [Parameter, EditorRequired] public IReadOnlyList<KycDocumentTypeOption> Types { get; set; } = [];
    [Parameter, EditorRequired] public string Root { get; set; } = string.Empty;
    [Parameter] public string? Token { get; set; }
    [Parameter] public long MaxFileBytes { get; set; } = 10_485_760;
    [Parameter] public bool RequiresAuthentication { get; set; } = true;
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public IReadOnlyList<KycSubmittedDocumentResponse> Documents { get; set; } = [];
    [Parameter] public EventCallback OnUploaded { get; set; }

    private readonly List<KycDocumentUploadRow> _rows = [];
    private string? _message;
    private Severity _severity = Severity.Info;
    private bool _busy;
    private bool Blocked => Disabled || _busy;

    protected override void OnParametersSet()
    {
        foreach (var type in Types)
        {
            if (!_rows.Any(row => !row.Additional && row.TypeKey == type.Key))
            {
                _rows.Add(new KycDocumentUploadRow(type.Key));
            }
        }
    }

    private string Name(KycDocumentUploadRow row)
    {
        return row.TypeKey == KycDocumentTypes.Other
            ? string.IsNullOrWhiteSpace(row.OtherName) ? "other document" : row.OtherName.Trim()
            : Types.FirstOrDefault(type => type.Key == row.TypeKey)?.Name ?? "document";
    }

    private bool HasType(KycDocumentUploadRow row)
    {
        return row.TypeKey == KycDocumentTypes.Other
            ? !string.IsNullOrWhiteSpace(row.OtherName) && row.OtherName.Trim().Length <= 80
            : Types.Any(type => type.Key == row.TypeKey);
    }

    private IEnumerable<KycSubmittedDocumentResponse> History(KycDocumentUploadRow row)
    {
        return Documents.Where(document => document.DocumentTypeKey == row.TypeKey
            && (row.TypeKey != KycDocumentTypes.Other
                || string.Equals(document.DocumentType, row.OtherName.Trim(), StringComparison.OrdinalIgnoreCase)));
    }

    private void AddRow()
    {
        if (!Blocked && _rows.Count < Types.Count + 20)
        {
            _rows.Add(new KycDocumentUploadRow(KycDocumentTypes.Other, additional: true));
        }
    }

    private void RemoveRow(KycDocumentUploadRow row)
    {
        if (!Blocked && row.Additional)
        {
            _rows.Remove(row);
        }
    }

    private void TypeChanged(KycDocumentUploadRow row)
    {
        row.File = null;
        row.InputKey = Guid.CreateVersion7();
        row.OtherName = string.Empty;
    }

    private void SelectFile(KycDocumentUploadRow row, InputFileChangeEventArgs args)
    {
        row.File = args.File;
    }

    private async Task UploadAsync(KycDocumentUploadRow row)
    {
        if (Blocked || row.File is null || !HasType(row))
        {
            return;
        }

        _busy = true;
        try
        {
            await using var stream = row.File.OpenReadStream(MaxFileBytes);
            using var form = new MultipartFormDataContent();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(fileContent, "File", row.File.Name);
            using var typeContent = new StringContent(row.TypeKey);
            form.Add(typeContent, "DocumentType");
            using var otherContent = row.TypeKey == KycDocumentTypes.Other ? new StringContent(row.OtherName.Trim()) : null;
            if (otherContent is not null)
            {
                form.Add(otherContent, "OtherDocumentName");
            }
            using var tokenContent = Token is not null ? new StringContent(Token) : null;
            if (tokenContent is not null)
            {
                form.Add(tokenContent, "Token");
            }

            var result = await Api.SendMultipartAsync<Guid>($"{Root}/documents", form,
                requiresAuthentication: RequiresAuthentication);
            _message = UserMessageSanitizer.Normalize(result.IsSuccess ? $"{Name(row)} uploaded for review." : result.Message,
                "The document could not be uploaded.");
            _severity = result.IsSuccess ? Severity.Success : Severity.Error;
            if (result.IsSuccess)
            {
                row.File = null;
                row.InputKey = Guid.CreateVersion7();
                await OnUploaded.InvokeAsync();
            }
        }
        catch (IOException)
        {
            _message = "The file could not be read. Check its size and try again.";
            _severity = Severity.Error;
        }
        finally
        {
            _busy = false;
        }
    }
}
