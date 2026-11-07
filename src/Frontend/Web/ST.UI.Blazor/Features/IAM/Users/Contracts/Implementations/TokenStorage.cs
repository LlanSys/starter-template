using ST.UI.Blazor.Features.IAM.Users.Models;
using ST.UI.Blazor.Features.IAM.Users.Contracts.Interfaces;
using ST.UI.Blazor.Features.IAM.Users.Implementations;
using ST.UI.Blazor.Logging;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;

using System.Security.Cryptography;

namespace ST.UI.Blazor.Features.IAM.Users.Contracts.Implementations;

internal sealed class TokenStorage(
    ProtectedLocalStorage storage,
    WorkspaceSelection selection,
    ProtectedSessionStorage workspaceStorage,
    IServerTokenStore serverStore,
    ILogger<TokenStorage> logger) : ITokenStorage
{
    private const string WorkspaceKey = "auth.workspace_tokens";
    private const string AccessKey = "auth.access_token";
    private const string RefreshKey = "auth.refresh_token";
    private const string SessionKey = "auth.session_id";

    private static string Key(string name, ST.UI.Blazor.Features.IAM.Users.Models.WorkspaceSelectionInfo? selected) => selected is null ? name : $"{name}.{selected.TenantId:N}";
    public async Task<bool> ClearAsync()
    {
        var browserCleared = false;
        try
        {
            var selected = await selection.GetAsync().ConfigureAwait(false);
            var workspace = await workspaceStorage.GetAsync<WorkspaceTokens>(WorkspaceKey).ConfigureAwait(false);
            if (workspace.Success && workspace.Value is not null)
                await workspaceStorage.DeleteAsync(WorkspaceKey).ConfigureAwait(false);
            else
            {
                await storage.DeleteAsync(Key(AccessKey, selected)).ConfigureAwait(false);
                await storage.DeleteAsync(Key(RefreshKey, selected)).ConfigureAwait(false);
                await storage.DeleteAsync(Key(SessionKey, selected)).ConfigureAwait(false);
            }
            browserCleared = true;
        }
        catch (Exception ex) when (IsBrowserStorageUnavailable(ex))
        {
            TokenStorageLogDefinitions.LogBrowserStorageClearUnavailable(logger, ex);
        }
        catch (Exception ex)
        {
            TokenStorageLogDefinitions.LogBrowserStorageClearUnavailable(logger, ex);
        }

        var serverCleared = await serverStore.ClearAsync().ConfigureAwait(false);
        return browserCleared || serverCleared;
    }

    public async Task<(string? AccessToken, string? RefreshToken, string? SessionId)> GetAsync()
    {
        try
        {
            var selected = await selection.GetAsync().ConfigureAwait(false);
            var workspace = await workspaceStorage.GetAsync<WorkspaceTokens>(WorkspaceKey).ConfigureAwait(false);
            if (workspace.Success && workspace.Value is not null)
            {
                var scoped = workspace.Value;
                await serverStore.SaveAsync(scoped.AccessToken, scoped.RefreshToken, scoped.SessionId).ConfigureAwait(false);
                return (scoped.AccessToken, scoped.RefreshToken, scoped.SessionId);
            }
            var access = await storage.GetAsync<string>(Key(AccessKey, selected)).ConfigureAwait(false);
            var refresh = await storage.GetAsync<string>(Key(RefreshKey, selected)).ConfigureAwait(false);
            var session = await storage.GetAsync<string>(Key(SessionKey, selected)).ConfigureAwait(false);

            var result = (
                AccessToken: access.Success ? access.Value : null,
                RefreshToken: refresh.Success ? refresh.Value : null,
                SessionId: session.Success ? session.Value : null);

            if (HasAnyValue(result))
            {
                await serverStore.SaveAsync(result.AccessToken, result.RefreshToken, result.SessionId).ConfigureAwait(false);
                return result;
            }

            return await serverStore.GetAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (IsBrowserStorageUnavailable(ex))
        {
            TokenStorageLogDefinitions.LogBrowserStorageReadUnavailable(logger, ex);
            return await serverStore.GetAsync().ConfigureAwait(false);
        }
    }

    public async Task<bool> SaveAsync(string? accessToken, string? refreshToken, string? sessionId)
    {
        var browserSaved = false;
        try
        {
            var selected = await selection.GetAsync().ConfigureAwait(false);
            var workspace = await workspaceStorage.GetAsync<WorkspaceTokens>(WorkspaceKey).ConfigureAwait(false);
            if (workspace.Success && workspace.Value is not null)
                await workspaceStorage.SetAsync(WorkspaceKey, new WorkspaceTokens(accessToken, refreshToken, sessionId)).ConfigureAwait(false);
            else
            {
                await storage.SetAsync(Key(AccessKey, selected), accessToken ?? "").ConfigureAwait(false);
                await storage.SetAsync(Key(RefreshKey, selected), refreshToken ?? "").ConfigureAwait(false);
                await storage.SetAsync(Key(SessionKey, selected), sessionId ?? "").ConfigureAwait(false);
            }
            browserSaved = true;
        }
        catch (Exception ex) when (IsBrowserStorageUnavailable(ex))
        {
            TokenStorageLogDefinitions.LogBrowserStorageWriteUnavailable(logger, ex);
        }

        var serverSaved = await serverStore.SaveAsync(accessToken, refreshToken, sessionId).ConfigureAwait(false);
        return browserSaved || serverSaved;
    }

    private static bool HasAnyValue((string? AccessToken, string? RefreshToken, string? SessionId) tokens)
        => !string.IsNullOrWhiteSpace(tokens.AccessToken)
            || !string.IsNullOrWhiteSpace(tokens.RefreshToken)
            || !string.IsNullOrWhiteSpace(tokens.SessionId);

    private static bool IsBrowserStorageUnavailable(Exception exception)
        => exception is
        OperationCanceledException or
        JSDisconnectedException or
        InvalidOperationException or
        CryptographicException;
}
