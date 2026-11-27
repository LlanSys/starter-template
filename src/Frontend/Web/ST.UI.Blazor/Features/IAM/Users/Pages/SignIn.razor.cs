using ST.SharedKernel.Features.ControlPlane.Tenants;
using ST.UI.Blazor.Features.Shared.Messaging;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using MudBlazor;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.UI.Blazor.Features.ControlPlane.Demo;
using ST.UI.Blazor.Features.IAM.Users.Models;
using ST.UI.Blazor.Features.IAM.Users.Implementations;
using ST.UI.Blazor.Logging;

namespace ST.UI.Blazor.Features.IAM.Users.Pages;

public partial class SignIn
{
    private const string SignInFormName = "iam-sign-in";

    [SupplyParameterFromForm(FormName = SignInFormName)]
    private SignInFormModel? FormModel
    {
        get; set;
    }

    [SupplyParameterFromQuery(Name = "workspace")]
    public string? WorkspaceIdentifier
    {
        get; set;
    }
    [SupplyParameterFromQuery(Name = "tenantId")]
    public Guid? TenantId
    {
        get; set;
    }
    private string _workspaceIdentifier = "";
    private WorkspaceSelectionInfo? _selectedWorkspace;
    private bool _changingWorkspace;
    private bool _hostLinkConflict;
    private bool _passkeyAvailable;

    private async Task ChangeWorkspaceAsync()
    {
        if (Frontend.IsBound)
        {
            return;
        }
        _changingWorkspace = true;
        _message = null;
        FormModel = new();
        // Cancel the browser's pending assertion before offering another workspace.
        try
        {
            await JSRuntime.InvokeVoidAsync("webauthn.cancelConditionalAssertion");
        }
        catch (JSDisconnectedException) { /* Navigation closed the circuit; no credential action follows. */ }
        _conditionalCorrelationId = Guid.Empty;
    }

    private void CancelWorkspaceChange()
    {
        _workspaceIdentifier = _selectedWorkspace?.Identifier ?? "";
        _changingWorkspace = false;
        _message = null;
    }

    private async Task ChooseWorkspaceAsync()
    {
        _isBusy = true;

        try
        {
            if (string.IsNullOrWhiteSpace(_workspaceIdentifier))
            {
                _message = "Enter the organization code from your sign-in link.";
                return;
            }

            if (await ResolveWorkspaceAsync(_workspaceIdentifier))
            {
                FormModel = new();
                _changingWorkspace = false;
                _message = null;
                Navigation.NavigateTo("/iam/sign-in?workspace=" + Uri.EscapeDataString(_selectedWorkspace!.Identifier), replace: true);
                await StartConditionalPasskeyAsync();
            }
        }
        finally { _isBusy = false; }
    }

    private async Task<bool> ResolveWorkspaceAsync(string? identifier = null, Guid? tenantId = null)
    {
        var query = tenantId.HasValue ? $"tenantId={tenantId}" : "identifier=" + Uri.EscapeDataString(identifier ?? "");
        var response = await Api.SendAsync<WorkspaceInfoResponse>(HttpMethod.Get, "api/v1/workspaces/resolve?" + query, requiresAuthentication: false, includeWorkspaceSelector: false);

        if (!response.IsSuccess || response.Data is null)
        {
            _message = "This organization is not available. Check the code in your sign-in link.";
            _isError = true;
            return false;
        }
        var workspace = response.Data;

        if (FrontendOrigin.TryNormalize(workspace.ClientOrigin, out var normalizedOrigin)
            && Uri.TryCreate(normalizedOrigin, UriKind.Absolute, out var clientOrigin)
            && !string.Equals(clientOrigin.GetLeftPart(UriPartial.Authority),
                new Uri(Navigation.Uri).GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
        {
            _redirectingWorkspace = true;
            Navigation.NavigateTo(new Uri(clientOrigin,
                "/iam/sign-in?workspace=" + Uri.EscapeDataString(workspace.Identifier)).AbsoluteUri, forceLoad: true);

            return false;
        }

        _selectedWorkspace = WorkspaceSelectionInfo.From(workspace);
        _workspaceIdentifier = workspace.Identifier;
        await WorkspaceSelection.SetAsync(_selectedWorkspace);
        return true;
    }
    private bool _canReturnToPlatform;
    private async Task RecoverPlatformAsync()
    {
        var backup = await SessionStorage.GetAsync<PlatformSessionBackup>("workspace.platform");

        if (!backup.Success || backup.Value is null)
        {
            return;
        }

        await SessionStorage.DeleteAsync("auth.workspace_tokens");
        await WorkspaceSelection.RestoreAsync(backup.Value.Workspace);

        if (!await Tokens.SaveAsync(backup.Value.AccessToken, backup.Value.RefreshToken, backup.Value.SessionId))
        {
            _message = "Could not restore the platform session. Please sign in again.";
            _isError = true;
            return;
        }

        await SessionStorage.DeleteAsync("workspace.platform");
        Navigation.NavigateTo("/control-panel", forceLoad: true);
    }
    private SsoProviderResponse[] _ssoProviders = [];

    private async Task SignInWithProviderAsync(string scheme)
    {
        if (string.IsNullOrWhiteSpace(BackendApiSettings.Value.PublicBaseUrl) || !await ResolveWorkspaceAsync(_workspaceIdentifier))
        {
            return;
        }

        var path = $"{BackendApiSettings.Value.PublicBaseUrl.TrimEnd('/')}/api/v{BackendApiSettings.Value.Version}/iam/auth/sso/login";
        var url = QueryHelpers.AddQueryString(path, new Dictionary<string, string?>
        {
            ["returnUrl"] = new Uri(new Uri(Navigation.BaseUri), "iam/sso-callback").AbsoluteUri,
            ["tenantId"] = _selectedWorkspace!.TenantId.ToString(),
            ["provider"] = scheme
        });

        Navigation.NavigateTo(url, forceLoad: true);
    }
    private bool _isBusy;
    private bool _isError;
    private string? _message;

    // Passkey Conditional UI state
    private DotNetObjectReference<SignIn>? _dotNetRef;
    private Guid _conditionalCorrelationId;

    protected override void OnInitialized()
    {
        FormModel ??= new();
    }

    private bool _ready;
    private bool _initializationFailed;
    private bool _providerLoadFailed;
    private bool _disposed;
    private bool _redirectingWorkspace;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await InitializeSignInAsync();
        }
    }

    private async Task InitializeSignInAsync()
    {
        _ready = false;
        _initializationFailed = false;
        StateHasChanged();
        var providerTask = LoadProvidersAsync();

        try
        {
            if (Frontend.Workspace is { } company &&
                ((TenantId.HasValue && TenantId != company.TenantId)
                    || (!string.IsNullOrWhiteSpace(WorkspaceIdentifier) && !string.Equals(WorkspaceIdentifier, company.Identifier, StringComparison.OrdinalIgnoreCase))))
            {
                _selectedWorkspace = WorkspaceSelectionInfo.From(company);
                _hostLinkConflict = true;
                _ready = true;
                StateHasChanged();
                return;
            }
            _hostLinkConflict = false;
            _canReturnToPlatform = (await SessionStorage.GetAsync<PlatformSessionBackup>("workspace.platform")).Success;
            _selectedWorkspace = await WorkspaceSelection.GetAsync();
            await AuthSession.InitializeAsync();
            var workspaceResolved = true;
            var requestedChange = (TenantId.HasValue && _selectedWorkspace?.TenantId != TenantId)
                || (!string.IsNullOrWhiteSpace(WorkspaceIdentifier)
                    && !string.Equals(_selectedWorkspace?.Identifier, WorkspaceIdentifier, StringComparison.OrdinalIgnoreCase));

            if (requestedChange || !AuthSession.IsAuthenticated)
            {
                if (await ResolveWorkspaceAsync(WorkspaceIdentifier ?? _selectedWorkspace?.Identifier, TenantId))
                {
                    // Select and revalidate the workspace's own stored session without reloading the document.
                    await AuthSession.RefreshAsync();
                }
                else
                {
                    if (_redirectingWorkspace)
                    {
                        return;
                    }
                    workspaceResolved = false;
                    _selectedWorkspace = null;
                    _changingWorkspace = !Frontend.IsBound;
                    _initializationFailed = Frontend.IsBound;
                    // Keep password entry usable when routing metadata is temporarily unavailable.
                    _workspaceIdentifier = WorkspaceIdentifier ?? string.Empty;
                }
            }

            if (AuthSession.IsAuthenticated && workspaceResolved)
            {
                Navigation.NavigateTo(AuthSession.MfaEnrollmentRequired ? "/iam/security" : "/dashboard");
                return;
            }

            if (workspaceResolved)
            {
                _workspaceIdentifier = _selectedWorkspace?.Identifier ?? WorkspaceIdentifier ?? string.Empty;
            }
            await providerTask;

            if (_disposed)
            {
                return;
            }

            _ready = true;
            StateHasChanged();

            if (workspaceResolved && _selectedWorkspace is not null)
            {
                await StartConditionalPasskeyAsync();
            }
        }
        catch (Exception exception) when (!_disposed)
        {
            SignInLogDefinitions.InitializationFailed(Logger, exception);
            _selectedWorkspace = null;
            _changingWorkspace = true;
            _initializationFailed = true;
            _ready = true;
            StateHasChanged();
        }
        finally
        {
            // Observe the independent request even when workspace routing navigates away.
            await providerTask;
        }
    }

    private async Task LoadProvidersAsync()
    {
        _providerLoadFailed = false;

        if (string.IsNullOrWhiteSpace(BackendApiSettings.Value.PublicBaseUrl))
        {
            return;
        }

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8), Clock);
            var providers = await Api.SendAsync<SsoProviderResponse[]>(HttpMethod.Get,
                $"api/v{BackendApiSettings.Value.Version}/iam/auth/sso/providers", requiresAuthentication: false,
                cancellationToken: timeout.Token, includeWorkspaceSelector: false);

            _ssoProviders = providers.Data ?? [];
            _providerLoadFailed = !providers.IsSuccess;
        }
        catch (Exception exception)
        {
            SignInLogDefinitions.ProvidersUnavailable(Logger, exception);
            _ssoProviders = [];
            _providerLoadFailed = true;
        }
    }

    private async Task StartConditionalPasskeyAsync()
    {
        try
        {
            // Fire both in parallel: feature-detect (JS/SignalR) and options fetch (HTTP) have zero dependency on each other
            var isSupportedTask = JSRuntime.InvokeAsync<bool>("webauthn.isConditionalMediationAvailable").AsTask();
            var optionsTask = AuthService.RequestPasskeyLoginOptionsAsync(new RequestPasskeyLoginOptionsRequest(null));

            await Task.WhenAll(isSupportedTask, optionsTask);

            var conditionalSupported = await isSupportedTask;
            var optionsResponse = await optionsTask;

            if (!optionsResponse.IsSuccess || optionsResponse.Data is null)
            {
                return;
            }

            var options = optionsResponse.Data.Options;
            var host = new Uri(Navigation.BaseUri).IdnHost;
            var rpId = options.TryGetProperty("rpId", out var relyingParty) ? relyingParty.GetString() : null;
            // Passkeys are cryptographically bound to their configured relying-party domain.
            // A localhost credential cannot be used on a company.localhost alias.
            _passkeyAvailable = !string.IsNullOrWhiteSpace(rpId)
                && (string.Equals(host, rpId, StringComparison.OrdinalIgnoreCase)
                    || (rpId.Contains('.', StringComparison.Ordinal) && host.EndsWith("." + rpId, StringComparison.OrdinalIgnoreCase)));

            await InvokeAsync(StateHasChanged);

            if (!_passkeyAvailable || !conditionalSupported)
            {
                return;
            }

            _conditionalCorrelationId = optionsResponse.Data.CorrelationId;
            _dotNetRef ??= DotNetObjectReference.Create(this);

            // Fire-and-forget: JS calls back into OnConditionalAssertionCompleted when the user taps a passkey
            _ = JSRuntime.InvokeVoidAsync(
                "webauthn.startConditionalAssertion",
                optionsResponse.Data.Options.GetRawText(),
                _dotNetRef).AsTask();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to start conditional passkey assertion.");
        }
    }

    [JSInvokable]
    public async Task RestartConditionalAssertionAsync()
    {
        // The browser ended passkey autofill (timeout, another tab's request or a dismissed picker).
        // Fresh options are requested because each challenge is single-use.
        if (_disposed || _changingWorkspace || _isBusy || _selectedWorkspace is null)
        {
            return;
        }

        await StartConditionalPasskeyAsync();
    }

    [JSInvokable]
    public async Task OnConditionalAssertionCompleted(JsonElement assertionResponse)
    {
        if (_changingWorkspace || _selectedWorkspace is null || _conditionalCorrelationId == Guid.Empty)
        {
            return;
        }

        try
        {
            var request = new LoginWithPasskeyRequest(
                null,
                _conditionalCorrelationId,
                assertionResponse);

            var response = await AuthSession.SignInWithPasskeyAsync(request);

            if (!response.IsSuccess || response.Data is null)
            {
                await InvokeAsync(() =>
                {
                    _isError = true;
                    _message = GetDisplayMessage(response.Message, "Passkey sign-in failed. Please try again.");
                    Snackbar.Add(_message, Severity.Error);
                    StateHasChanged();
                });

                return;
            }

            await InvokeAsync(() =>
            {
                Navigation.NavigateTo(AuthSession.MfaEnrollmentRequired ? "/iam/security" : "/dashboard");
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error completing conditional passkey sign-in.");
        }
    }

    private async Task SignInAsync()
    {
        _isBusy = true;
        _message = null;
        _isError = false;

        try
        {
            if (!await ResolveWorkspaceAsync(_workspaceIdentifier))
            {
                return;
            }

            var form = FormModel ??= new();
            var request = new PasswordLoginRequest(form.UserName, form.Password, form.RememberMe, form.ReturnUrl, "blazor-server");
            var response = await AuthSession.SignInAsync(request);

            if (!response.IsSuccess || response.Data is null)
            {
                _isError = true;
                _message = GetDisplayMessage(response.Message, "We could not sign you in. Please check your username and password.");
                Snackbar.Add(_message, Severity.Error);
                return;
            }

            if (response.Data.Requires2FA)
            {
                Navigation.NavigateTo($"/iam/verify-2fa?userId={Uri.EscapeDataString(response.Data.UserId)}");
                return;
            }

            if (!AuthSession.IsAuthenticated)
            {
                _isError = true;
                _message = GetDisplayMessage(
                    AuthSession.LastError,
                    "Sign in succeeded, but we could not load your profile. Please confirm the API is running and try again.");

                Snackbar.Add(_message, Severity.Warning);
                return;
            }

            Navigation.NavigateTo(AuthSession.MfaEnrollmentRequired ? "/iam/security" : "/dashboard");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error while signing in.");
            _isError = true;
            _message = "Something went wrong while signing you in. Please try again. If this continues, check the UI logs for the sign-in error.";
            Snackbar.Add(_message, Severity.Error);
        }
        finally
        {
            _isBusy = false;
        }
    }

    private static string GetDisplayMessage(string? message, string fallback)
    {
        if (string.IsNullOrWhiteSpace(message)
            || string.Equals(message, "Operation Successful", StringComparison.OrdinalIgnoreCase))
        {
            return fallback;
        }

        return UserMessageSanitizer.Normalize(message, fallback);
    }

    private async Task SignInWithPasskeyAsync()
    {
        if (!await ResolveWorkspaceAsync(_workspaceIdentifier))
        {
            return;
        }

        _isBusy = true;
        _message = null;
        _isError = false;

        // Cancel any ongoing conditional UI flow
        try
        {
            await JSRuntime.InvokeVoidAsync("webauthn.cancelConditionalAssertion");
        }
        catch { /* ignore */ }

        try
        {
            var form = FormModel ??= new();
            var optionsResponse = await AuthService.RequestPasskeyLoginOptionsAsync(
                new RequestPasskeyLoginOptionsRequest(string.IsNullOrWhiteSpace(form.UserName) ? null : form.UserName));

            if (!optionsResponse.IsSuccess || optionsResponse.Data is null)
            {
                _isError = true;
                _message = GetDisplayMessage(optionsResponse.Message, "Could not start passkey login.");
                Snackbar.Add(_message, Severity.Error);
                return;
            }

            var correlationId = optionsResponse.Data.CorrelationId;
            var assertionJson = await JSRuntime.InvokeAsync<string>(
                "webauthn.startExplicitAssertion",
                optionsResponse.Data.Options.GetRawText());

            var assertion = JsonDocument.Parse(assertionJson).RootElement;

            var request = new LoginWithPasskeyRequest(
                string.IsNullOrWhiteSpace(form.UserName) ? null : form.UserName,
                correlationId,
                assertion);

            var response = await AuthSession.SignInWithPasskeyAsync(request);

            if (!response.IsSuccess || response.Data is null)
            {
                _isError = true;
                _message = GetDisplayMessage(response.Message, "We could not sign you in with your passkey.");
                Snackbar.Add(_message, Severity.Error);
                return;
            }

            if (response.Data.Requires2FA)
            {
                Navigation.NavigateTo($"/iam/verify-2fa?userId={Uri.EscapeDataString(response.Data.UserId)}");
                return;
            }

            if (!AuthSession.IsAuthenticated)
            {
                _isError = true;
                _message = GetDisplayMessage(
                    AuthSession.LastError,
                    "Sign in succeeded, but we could not load your profile. Please confirm the API is running and try again.");

                Snackbar.Add(_message, Severity.Warning);
                return;
            }

            Navigation.NavigateTo(AuthSession.MfaEnrollmentRequired ? "/iam/security" : "/dashboard");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error while signing in with passkey.");
            _isError = true;
            _message = "Passkey sign-in was cancelled or failed.";
            Snackbar.Add(_message, Severity.Error);

            // Restart conditional UI for the next attempt
            _ = StartConditionalPasskeyAsync();
        }
        finally
        {
            _isBusy = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;

        try
        {
            await JSRuntime.InvokeVoidAsync("webauthn.cancelConditionalAssertion");
        }
        catch (JSDisconnectedException) { /* circuit torn down */ }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error cancelling conditional passkey on dispose.");
        }

        _dotNetRef?.Dispose();
    }
}
