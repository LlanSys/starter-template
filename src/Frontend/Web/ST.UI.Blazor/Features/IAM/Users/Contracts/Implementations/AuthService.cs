using System.Net;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Common.Enums;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.UI.Blazor.Configuration;
using ST.UI.Blazor.Features.Shared.BackendApi;
using ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;
using ST.UI.Blazor.Features.IAM.Users.Contracts.Interfaces;
using Microsoft.Extensions.Options;

namespace ST.UI.Blazor.Features.IAM.Users.Contracts.Implementations;

internal sealed class AuthService(IBackendApiClient apiClient, ITokenStorage storage, IOptions<BackendApiSettings> apiSettings) : IAuthService
{
    private readonly BackendApiSettings _apiSettings = apiSettings.Value;

    public async Task<AppResponse<LoginResponse>> LoginAsync(LoginRequest loginRequest)
    {
        ArgumentNullException.ThrowIfNull(loginRequest);

        var response = await apiClient.SendAsync<LoginResponse>(
            HttpMethod.Post,
            Format(_apiSettings.Endpoints.Iam.Auth.Login),
            loginRequest,
            requiresAuthentication: false,
            unavailableMessage: "The identity service is unavailable. Please try again.",
            timeoutMessage: "The identity service timed out. Please try again.").ConfigureAwait(false);

        if (response.IsSuccess && response.Data is { IsAuthenticated: true, Requires2FA: false } login)
        {
            await storage.SaveAsync(login.Token, login.RefreshToken, login.SessionId).ConfigureAwait(false);
        }

        return response;
    }

    public async Task<AppResponse<LoginResponse>> ExchangeSsoCodeAsync(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var request = new ExchangeSsoCodeRequest(code);

        var response = await apiClient.SendAsync<LoginResponse>(
            HttpMethod.Post,
            Format("iam/auth/sso/exchange"),
            request,
            requiresAuthentication: false,
            unavailableMessage: "The identity service is unavailable. Please try again.",
            timeoutMessage: "The identity service timed out. Please try again.").ConfigureAwait(false);

        if (response.IsSuccess && response.Data is { IsAuthenticated: true, Requires2FA: false } login)
        {
            await storage.SaveAsync(login.Token, login.RefreshToken, login.SessionId).ConfigureAwait(false);
        }

        return response;
    }

    public async Task<AppResponse<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await apiClient.SendAsync<RefreshTokenResponse>(
            HttpMethod.Post,
            Format(_apiSettings.Endpoints.Iam.Auth.RefreshToken),
            request,
            requiresAuthentication: false,
            unavailableMessage: "The identity service is unavailable. Please try again.",
            timeoutMessage: "The identity service timed out. Please try again.").ConfigureAwait(false);

        if (response.IsSuccess && response.Data is not null)
        {
            await storage.SaveAsync(response.Data.Token, response.Data.RefreshToken, response.Data.SessionId).ConfigureAwait(false);
        }

        return response;
    }

    public Task<AppResponse<ForgotPasswordResponse>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return SendRecoveryAsync<ForgotPasswordResponse>(
            Format(_apiSettings.Endpoints.Iam.Auth.ForgotPassword),
            request);
    }

    public Task<AppResponse<PasswordResetOtpVerificationResponse>> VerifyPasswordResetOtpAsync(VerifyPasswordResetOtpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return SendRecoveryAsync<PasswordResetOtpVerificationResponse>(
            Format(_apiSettings.Endpoints.Iam.Auth.VerifyPasswordResetOtp),
            request);
    }

    public Task<AppResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return SendRecoveryAsync<bool>(
            Format(_apiSettings.Endpoints.Iam.Auth.ResetPassword),
            request);
    }

    public Task<AppResponse<bool>> ChangePasswordAsync(ChangePasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return SendWithRefreshDataAsync<bool>(
            HttpMethod.Post,
            Format(_apiSettings.Endpoints.Iam.Auth.ChangePassword),
            request);
    }

    public Task<AppResponse<CurrentUserResponse>> GetCurrentUserAsync()
        => SendWithRefreshAsync<CurrentUserResponse>(HttpMethod.Get, Format(_apiSettings.Endpoints.Iam.Auth.CurrentUser));

    public Task<AppResponse<FileContentResponse>> GetProfilePictureAsync()
        => SendFileWithRefreshAsync(Format(_apiSettings.Endpoints.Iam.Auth.ProfilePictureContent));

    public Task<AppResponse<ProfilePictureResponse>> UpdateProfilePictureAsync(byte[] content, string fileName, string contentType)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        return SendMultipartWithRefreshAsync<ProfilePictureResponse>(
            Format(_apiSettings.Endpoints.Iam.Auth.UpdateProfilePicture),
            () =>
            {
                var multipart = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(content);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                multipart.Add(fileContent, "file", fileName);
                return multipart;
            });
    }

    public Task<AppResponse<TwoFactorSetupInfo>> InitiateTotpSetupAsync()
        => SendWithRefreshAsync<TwoFactorSetupInfo>(HttpMethod.Post, Format(_apiSettings.Endpoints.Iam.Users.InitiateTotpSetup));

    public Task<AppResponse<VerifyOtpResponse>> VerifyTotpAsync(VerifyOtpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return apiClient.SendAsync<VerifyOtpResponse>(
            HttpMethod.Post,
            Format(_apiSettings.Endpoints.Iam.Users.VerifyTotp),
            request,
            requiresAuthentication: false,
            unavailableMessage: "The identity service is unavailable. Please try again.",
            timeoutMessage: "The identity service timed out. Please try again.");
    }

    public Task<AppResponse<bool>> DisableTotpAsync()
        => SendWithRefreshAsync<bool>(
            HttpMethod.Post,
            Format(_apiSettings.Endpoints.Iam.Users.DisableTotp));

    public Task<AppResponse<OtpStatusResponse>> GetTotpStatusAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        return SendWithRefreshAsync<OtpStatusResponse>(
            HttpMethod.Get,
            Format(_apiSettings.Endpoints.Iam.Users.TotpStatus, ("userId", userId)));
    }

    public Task<AppResponse<System.Text.Json.JsonElement>> RequestPasskeyRegistrationOptionsAsync()
        => SendWithRefreshAsync<System.Text.Json.JsonElement>(HttpMethod.Post, Format(_apiSettings.Endpoints.Iam.Auth.PasskeyRegistrationOptions));

    public Task<AppResponse<bool>> RegisterPasskeyAsync(RegisterPasskeyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return SendWithRefreshDataAsync<bool>(
            HttpMethod.Post,
            Format(_apiSettings.Endpoints.Iam.Auth.PasskeyRegister),
            request);
    }

    public Task<AppResponse<PasskeyLoginOptionsResponse>> RequestPasskeyLoginOptionsAsync(RequestPasskeyLoginOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return apiClient.SendAsync<PasskeyLoginOptionsResponse>(
            HttpMethod.Post,
            Format(_apiSettings.Endpoints.Iam.Auth.PasskeyLoginOptions),
            request,
            requiresAuthentication: false,
            unavailableMessage: "The identity service is unavailable. Please try again.",
            timeoutMessage: "The identity service timed out. Please try again.");
    }

    public async Task<AppResponse<LoginResponse>> LoginWithPasskeyAsync(LoginWithPasskeyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var response = await apiClient.SendAsync<LoginResponse>(
            HttpMethod.Post,
            Format(_apiSettings.Endpoints.Iam.Auth.PasskeyLogin),
            request,
            requiresAuthentication: false,
            unavailableMessage: "The identity service is unavailable. Please try again.",
            timeoutMessage: "The identity service timed out. Please try again.").ConfigureAwait(false);

        if (response.IsSuccess && response.Data is { IsAuthenticated: true, Requires2FA: false } login)
        {
            await storage.SaveAsync(login.Token, login.RefreshToken, login.SessionId).ConfigureAwait(false);
        }

        return response;
    }

    public Task<AppResponse<IReadOnlyList<PasskeyResponse>>> GetPasskeysAsync()
        => SendWithRefreshAsync<IReadOnlyList<PasskeyResponse>>(
            HttpMethod.Get,
            Format(_apiSettings.Endpoints.Iam.Auth.Passkeys));

    public async Task<AppResponse<bool>> LogoutAsync()
    {
        try
        {
            _ = await SendWithRefreshAsync<bool>(HttpMethod.Post, Format(_apiSettings.Endpoints.Iam.Auth.Logout))
                .ConfigureAwait(false);
        }
        finally
        {
            await storage.ClearAsync().ConfigureAwait(false);
        }

        return AppResponses.Success("Signed out", true);
    }

    private async Task<AppResponse<T>> SendWithRefreshAsync<T>(HttpMethod method, string endpoint)
    {
        var response = await SendAuthenticatedAsync<T>(method, endpoint).ConfigureAwait(false);
        if (response.Error?.Type != ErrorType.Unauthorized)
        {
            return response;
        }

        var (accessToken, refreshToken, _) = await storage.GetAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return response;
        }

        var refreshResponse = await RefreshTokenAsync(new RefreshTokenRequest(accessToken, refreshToken)).ConfigureAwait(false);
        if (!refreshResponse.IsSuccess)
        {
            await storage.ClearAsync().ConfigureAwait(false);
            return AppResponses.Failure<T>(refreshResponse.Message ?? "Your session has expired. Please sign in again.");
        }

        return await SendAuthenticatedAsync<T>(method, endpoint).ConfigureAwait(false);
    }

    private Task<AppResponse<T>> SendAuthenticatedAsync<T>(HttpMethod method, string endpoint)
        => apiClient.SendAsync<T>(
            method,
            endpoint,
            requiresAuthentication: true,
            unavailableMessage: "The identity service is unavailable. Please try again.",
            timeoutMessage: "The identity service timed out. Please try again.");

    private async Task<AppResponse<T>> SendWithRefreshDataAsync<T>(HttpMethod method, string endpoint, object data)
    {
        var response = await apiClient.SendAsync<T>(
            method,
            endpoint,
            data,
            requiresAuthentication: true,
            unavailableMessage: "The identity service is unavailable. Please try again.",
            timeoutMessage: "The identity service timed out. Please try again.").ConfigureAwait(false);

        if (response.Error?.Type != ErrorType.Unauthorized)
        {
            return response;
        }

        var (accessToken, refreshToken, _) = await storage.GetAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return response;
        }

        var refreshResponse = await RefreshTokenAsync(new RefreshTokenRequest(accessToken, refreshToken)).ConfigureAwait(false);
        if (!refreshResponse.IsSuccess)
        {
            await storage.ClearAsync().ConfigureAwait(false);
            return AppResponses.Failure<T>(refreshResponse.Message ?? "Your session has expired. Please sign in again.");
        }

        return await apiClient.SendAsync<T>(
            method,
            endpoint,
            data,
            requiresAuthentication: true,
            unavailableMessage: "The identity service is unavailable. Please try again.",
            timeoutMessage: "The identity service timed out. Please try again.").ConfigureAwait(false);
    }

    private Task<AppResponse<T>> SendRecoveryAsync<T>(string endpoint, object request)
        => apiClient.SendAsync<T>(
            HttpMethod.Post,
            endpoint,
            request,
            requiresAuthentication: false,
            unavailableMessage: "Password recovery is temporarily unavailable. Please try again.",
            timeoutMessage: "Password recovery timed out. Please try again.");

    private async Task<AppResponse<T>> SendMultipartWithRefreshAsync<T>(
        string endpoint,
        Func<MultipartFormDataContent> contentFactory)
    {
        using var content = contentFactory();
        var response = await apiClient.SendMultipartAsync<T>(
                endpoint,
                content,
                requiresAuthentication: true,
                unavailableMessage: "The identity service is unavailable. Please try again.",
                timeoutMessage: "The identity service timed out. Please try again.")
            .ConfigureAwait(false);

        if (response.Error?.Type != ErrorType.Unauthorized)
        {
            return response;
        }

        var (accessToken, refreshToken, _) = await storage.GetAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return response;
        }

        var refreshResponse = await RefreshTokenAsync(new RefreshTokenRequest(accessToken, refreshToken)).ConfigureAwait(false);
        if (!refreshResponse.IsSuccess)
        {
            await storage.ClearAsync().ConfigureAwait(false);
            return AppResponses.Failure<T>(refreshResponse.Message ?? "Your session has expired. Please sign in again.");
        }

        using var retryContent = contentFactory();
        return await apiClient.SendMultipartAsync<T>(
                endpoint,
                retryContent,
                requiresAuthentication: true,
                unavailableMessage: "The identity service is unavailable. Please try again.",
                timeoutMessage: "The identity service timed out. Please try again.")
            .ConfigureAwait(false);
    }

    private async Task<AppResponse<FileContentResponse>> SendFileWithRefreshAsync(string endpoint)
    {
        var response = await apiClient.SendFileAsync(
            endpoint,
            unavailableMessage: "The profile picture service is unavailable. Please try again.",
            timeoutMessage: "The profile picture request timed out. Please try again.").ConfigureAwait(false);

        if (response.Error?.Type != ErrorType.Unauthorized)
        {
            return response;
        }

        var (accessToken, refreshToken, _) = await storage.GetAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return response;
        }

        var refreshResponse = await RefreshTokenAsync(new RefreshTokenRequest(accessToken, refreshToken)).ConfigureAwait(false);
        if (!refreshResponse.IsSuccess)
        {
            await storage.ClearAsync().ConfigureAwait(false);
            return AppResponses.Failure<FileContentResponse>(refreshResponse.Message ?? "Your session has expired. Please sign in again.");
        }

        return await apiClient.SendFileAsync(
            endpoint,
            unavailableMessage: "The profile picture service is unavailable. Please try again.",
            timeoutMessage: "The profile picture request timed out. Please try again.").ConfigureAwait(false);
    }

    private string Format(string endpoint, params (string Key, string Value)[] parameters)
        => EndpointFormatter.Format(endpoint, _apiSettings.Version, parameters.ToDictionary(static item => item.Key, static item => item.Value));
}
