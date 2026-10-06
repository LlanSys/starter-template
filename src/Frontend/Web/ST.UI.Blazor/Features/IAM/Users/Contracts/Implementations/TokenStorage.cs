using ST.UI.Blazor.Features.IAM.Users.Contracts.Interfaces;
using ST.UI.Blazor.Features.IAM.Users.Implementations;
using ST.UI.Blazor.Logging;
using ST.UI.Rcl.Features.IAM.Users.Contracts.Interfaces;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;

using System.Security.Cryptography;

namespace ST.UI.Blazor.Features.IAM.Users.Contracts.Implementations;

internal sealed class TokenStorage(
    ProtectedLocalStorage storage,
    IServerTokenStore serverStore,
    ILogger<TokenStorage> logger) : ITokenStorage
{
    private const string AccessKey = "auth.access_token";
    private const string RefreshKey = "auth.refresh_token";
    private const string SessionKey = "auth.session_id";

    public async Task<bool> ClearAsync()
    {
        var browserCleared = false;
        try
        {
            await storage.DeleteAsync(AccessKey).ConfigureAwait(false);
            await storage.DeleteAsync(RefreshKey).ConfigureAwait(false);
            await storage.DeleteAsync(SessionKey).ConfigureAwait(false);
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
            var access = await storage.GetAsync<string>(AccessKey).ConfigureAwait(false);
            var refresh = await storage.GetAsync<string>(RefreshKey).ConfigureAwait(false);
            var session = await storage.GetAsync<string>(SessionKey).ConfigureAwait(false);

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
            await storage.SetAsync(AccessKey, accessToken ?? "").ConfigureAwait(false);
            await storage.SetAsync(RefreshKey, refreshToken ?? "").ConfigureAwait(false);
            await storage.SetAsync(SessionKey, sessionId ?? "").ConfigureAwait(false);
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
