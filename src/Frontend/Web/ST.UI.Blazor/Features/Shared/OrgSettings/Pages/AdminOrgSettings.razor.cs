using Microsoft.AspNetCore.Components;
using MudBlazor;
using ST.SharedKernel.Features.Shared.OrgSettings.Dtos;
using ST.UI.Blazor.Components.Common;
using ST.UI.Blazor.Features.Shared.OrgSettings.Components;
using ST.UI.Blazor.Features.Shared.Messaging;
namespace ST.UI.Blazor.Features.Shared.OrgSettings.Pages;

public partial class AdminOrgSettings
{
    private IReadOnlyList<CatalogOrgSettingResponse> _settings = [];
    private IReadOnlyList<OrgSettingChangeResponse> _history = [];
    private bool _loading;
    private string? _message;
    private DateTimeOffset _previewInstant;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private static string Label(CatalogOrgSettingResponse setting, string value)
    {
        return setting.Definition.Options.FirstOrDefault(option => option.Value == value)?.Label ?? value;
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _message = null;
        _previewInstant = Clock.GetUtcNow();
        var response = await OrgSettingsService.GetCatalogueAsync();
        _settings = response.IsSuccess ? response.Data ?? [] : [];
        _history = [];

        if (!response.IsSuccess)
        {
            _message = UserMessageSanitizer.Normalize(response.Message, "Could not load organization settings.");
        }
        else
        {
            var history = await OrgSettingsService.GetHistoryAsync();
            _history = history.IsSuccess ? history.Data ?? [] : [];

            if (!history.IsSuccess)
            {
                _message = "Recent changes could not be loaded.";
            }
            await RefreshPresentationAsync();
        }
        _loading = false;
    }

    private async Task RefreshPresentationAsync()
    {
        var response = await OrgSettingsService.GetPresentationAsync();

        if (response.IsSuccess && response.Data is not null)
        {
            Presentation.Apply(response.Data);
        }
    }

    private async Task EditAsync(CatalogOrgSettingResponse setting)
    {
        var parameters = new DialogParameters { [nameof(OrgSettingFormDialog.Setting)] = setting };
        var dialog = await DialogService.ShowAsync<OrgSettingFormDialog>("Edit " + setting.Definition.Label, parameters,
            new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true });

        if (await dialog.Result is { Canceled: false })
        {
            await LoadAsync();
        }
    }

    private async Task ResetAsync(CatalogOrgSettingResponse setting)
    {
        var parameters = new DialogParameters<ConfirmDialog>
        {
            { dialog => dialog.Message, $"Reset {setting.Definition.Label} to {Label(setting, setting.Definition.DefaultValue)} for this workspace?" },
            { dialog => dialog.ConfirmText, "Reset To Default" }
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Reset Setting", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true });

        if (await dialog.Result is not { Canceled: false })
        {
            return;
        }
        var response = await OrgSettingsService.ResetAsync(setting.Definition.Key,
            new SaveCatalogOrgSettingRequest(setting.Value, setting.Revision));

        Snackbar.Add(UserMessageSanitizer.Normalize(response.Message, response.IsSuccess ? "Default restored." : "Could not reset this setting."),
            response.IsSuccess ? Severity.Success : Severity.Error);

        await LoadAsync();
    }
}
