using ST.UI.Blazor.Features.IAM.Users.Contracts.Interfaces;
using ST.UI.Rcl.Features.IAM.Users.Contracts.Interfaces;

namespace ST.UI.Blazor.Features.IAM.Users.Implementations;

internal sealed class ServerTokenStore : IServerTokenStore
{
    /// <summary>
    /// In-memory per-circuit token store. Intended for server-side background code
    /// to access tokens when JS interop is not available.
    /// Register as Scoped in DI to keep tokens per-circuit.
    /// </summary>
    // Simple in-memory per-circuit store. Register as Scoped in DI.
    private string? _accessToken;
    private string? _refreshToken;
    private string? _sessionId;

    public Task<bool> ClearAsync()
    {
        _accessToken = null;
        _refreshToken = null;
        _sessionId = null;
        return Task.FromResult(true);
    }

    public Task<(string? AccessToken, string? RefreshToken, string? SessionId)> GetAsync()
    {
        return Task.FromResult((AccessToken: _accessToken, RefreshToken: _refreshToken, SessionId: _sessionId));
    }

    public Task<bool> SaveAsync(string? accessToken, string? refreshToken, string? sessionId)
    {
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        _sessionId = sessionId;
        return Task.FromResult(true);
    }
}
