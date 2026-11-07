using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Common.Enums;
using ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;
using ST.UI.Blazor.Features.Shared.Messaging;
using ST.UI.Blazor.Logging;
using ST.UI.Blazor.Features.IAM.Users.Contracts.Interfaces;
using ST.UI.Blazor.Configuration;
using Microsoft.Extensions.Options;
using ST.UI.Blazor.Features.IAM.Users.Implementations;
using System.Net;

namespace ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Implementations;

internal sealed class BackendApiClient(
    HttpClient httpClient,
    ITokenStorage storage,
    WorkspaceSelection selection,
    IOptions<BackendApiSettings> apiSettings,
    ILogger<BackendApiClient> logger,
    TimeProvider timeProvider) : IBackendApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly BackendApiSettings _apiSettings = apiSettings.Value;

    public async Task<AppResponse<T>> SendAsync<T>(
        HttpMethod method,
        string endpoint,
        object? request = null,
        bool requiresAuthentication = true,
        string? unavailableMessage = null,
        string? timeoutMessage = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        string? accessToken = null;
        string? sessionId = null;
        if (requiresAuthentication)
        {
            (accessToken, _, sessionId) = await storage.GetAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return AppResponses.Failure<T>("Please sign in to continue.");
            }
        }

        var maxAttempts = Math.Max(1, _apiSettings.TransientRetryCount + 1);
        var delay = TimeSpan.FromMilliseconds(_apiSettings.TransientRetryDelayMilliseconds);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var message = CreateRequest(method, endpoint, request);
            var selected = await selection.GetAsync().ConfigureAwait(false);
            if (selected is not null) message.Headers.TryAddWithoutValidation(_apiSettings.TenantHeaderName, selected.TenantId.ToString());

            if (requiresAuthentication)
            {
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                if (!string.IsNullOrWhiteSpace(sessionId))
                {
                    message.Headers.TryAddWithoutValidation("X-Session-Id", sessionId);
                }
            }

            try
            {
                using var response = await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
                return await ReadAppResponseAsync<T>(response, endpoint).ConfigureAwait(false);
            }
            catch (HttpRequestException ex) when (ShouldRetryTransportFailure(attempt, maxAttempts, cancellationToken))
            {
                BackendApiLogDefinitions.LogRequestRetrying(logger, method.Method, endpoint, attempt, maxAttempts, (int)delay.TotalMilliseconds, ex);
                await Task.Delay(delay, timeProvider, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                BackendApiLogDefinitions.LogRequestFailed(logger, method.Method, endpoint, ex);
                return AppResponses.Failure<T>(unavailableMessage ?? "The backend service is unavailable. Please try again.");
            }
            catch (TaskCanceledException ex)
            {
                BackendApiLogDefinitions.LogRequestTimedOut(logger, method.Method, endpoint, ex);
                return AppResponses.Failure<T>(timeoutMessage ?? "The backend service timed out. Please try again.");
            }
        }

        return AppResponses.Failure<T>(unavailableMessage ?? "The backend service is unavailable. Please try again.");
    }

    public async Task<AppResponse<T>> SendMultipartAsync<T>(
        string endpoint,
        MultipartFormDataContent content,
        bool requiresAuthentication = true,
        string? unavailableMessage = null,
        string? timeoutMessage = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentNullException.ThrowIfNull(content);

        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = content
        };

        if (requiresAuthentication)
        {
            var (accessToken, _, sessionId) = await storage.GetAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return AppResponses.Failure<T>("Please sign in to continue.");
            }

            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                message.Headers.TryAddWithoutValidation("X-Session-Id", sessionId);
            }
        }

        try
        {
            using var response = await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
            return await ReadAppResponseAsync<T>(response, endpoint).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            BackendApiLogDefinitions.LogRequestFailed(logger, HttpMethod.Post.Method, endpoint, ex);
            return AppResponses.Failure<T>(unavailableMessage ?? "The backend service is unavailable. Please try again.");
        }
        catch (TaskCanceledException ex)
        {
            BackendApiLogDefinitions.LogRequestTimedOut(logger, HttpMethod.Post.Method, endpoint, ex);
            return AppResponses.Failure<T>(timeoutMessage ?? "The backend service timed out. Please try again.");
        }
    }

    public async Task<AppResponse<FileContentResponse>> SendFileAsync(
        string endpoint,
        bool requiresAuthentication = true,
        string? unavailableMessage = null,
        string? timeoutMessage = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        using var message = new HttpRequestMessage(HttpMethod.Get, endpoint);
        if (requiresAuthentication)
        {
            var (accessToken, _, sessionId) = await storage.GetAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return AppResponses.Failure<FileContentResponse>("Please sign in to continue.");
            }

            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                message.Headers.TryAddWithoutValidation("X-Session-Id", sessionId);
            }
        }

        try
        {
            using var response = await httpClient.SendAsync(
                message,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var userMessage = UserMessageSanitizer.Normalize(
                    await ReadErrorMessageAsync(response, cancellationToken).ConfigureAwait(false),
                    "The profile picture could not be loaded.");

                return response.StatusCode == HttpStatusCode.Unauthorized
                    ? AppResponses.Failure<FileContentResponse>(new AppError(
                        ErrorType.Unauthorized,
                        "UNAUTHORIZED",
                        userMessage))
                    : AppResponses.Failure<FileContentResponse>(userMessage);
            }

            var content = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            return AppResponses.Success("Profile picture loaded.", new FileContentResponse(content, contentType));
        }
        catch (HttpRequestException ex)
        {
            BackendApiLogDefinitions.LogRequestFailed(logger, HttpMethod.Get.Method, endpoint, ex);
            return AppResponses.Failure<FileContentResponse>(unavailableMessage ?? "The backend service is unavailable. Please try again.");
        }
        catch (TaskCanceledException ex)
        {
            BackendApiLogDefinitions.LogRequestTimedOut(logger, HttpMethod.Get.Method, endpoint, ex);
            return AppResponses.Failure<FileContentResponse>(timeoutMessage ?? "The backend service timed out. Please try again.");
        }
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string endpoint, object? request)
        => new(method, endpoint)
        {
            Content = request is null ? null : JsonContent.Create(request, options: JsonOptions)
        };

    private static bool ShouldRetryTransportFailure(int attempt, int maxAttempts, CancellationToken cancellationToken)
        => attempt < maxAttempts && !cancellationToken.IsCancellationRequested;

    private async Task<AppResponse<T>> ReadAppResponseAsync<T>(HttpResponseMessage response, string endpoint)
    {
        var appResponse = await TryReadAppResponseAsync<T>(response, endpoint).ConfigureAwait(false);
        if (appResponse is not null)
        {
            return appResponse with
            {
                Message = response.IsSuccessStatusCode
                    ? UserMessageSanitizer.NormalizeNullable(appResponse.Message, "Operation completed.")
                    : UserMessageSanitizer.Normalize(
                        appResponse.Message,
                        "The request could not be completed. Please try again or contact support if the problem persists.")
            };
        }

        return AppResponses.Failure<T>(
            response.IsSuccessStatusCode
                ? "The backend service returned an empty response."
                : UserMessageSanitizer.Normalize(
                    await ReadErrorMessageAsync(response).ConfigureAwait(false),
                    "The request could not be completed. Please try again or contact support if the problem persists."));
    }

    private async Task<AppResponse<T>?> TryReadAppResponseAsync<T>(HttpResponseMessage response, string endpoint)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            using var document = JsonDocument.Parse(content);
            if (!document.RootElement.TryGetProperty("isSuccess", out _) &&
                !document.RootElement.TryGetProperty("successful", out _))
            {
                return null;
            }

            return JsonSerializer.Deserialize<AppResponse<T>>(content, JsonOptions);
        }
        catch (JsonException ex)
        {
            BackendApiLogDefinitions.LogUnreadableResponse(logger, response.StatusCode.ToString(), endpoint, ex);
            return null;
        }
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(content))
        {
            return "The backend service rejected the request.";
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            var validationErrors = TryGetValidationErrors(root);
            if (!string.IsNullOrWhiteSpace(validationErrors))
            {
                return validationErrors;
            }

            // If it's a Validation problem, we should prioritize the specific field errors.
            // Prioritized list of common error keys
            ReadOnlySpan<string> errorKeys = ["message", "detail", "title", "error"];
            foreach (var key in errorKeys)
            {
                var value = TryGetString(root, key);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return TryGetSessionInvalidMessage(root) ?? "The backend service rejected the request.";
        }
        catch (JsonException)
        {
            return content.Length <= 200
                ? UserMessageSanitizer.Normalize(content, "The backend service rejected the request.")
                : "The backend service rejected the request.";
        }
    }

    private static string? TryGetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static string? TryGetSessionInvalidMessage(JsonElement element)
    {
        var code = TryGetString(element, "code");
        if (!string.Equals(code, "SESSION_INVALID", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var reason = TryGetString(element, "reason");
        return string.IsNullOrWhiteSpace(reason)
            ? "Your session has expired. Please sign in again."
            : $"Your session has expired: {reason}. Please sign in again.";
    }

    private static string? TryGetValidationErrors(JsonElement element)
    {
        if (!element.TryGetProperty("errors", out var errors))
        {
            return null;
        }

        if (errors.ValueKind == JsonValueKind.Array)
        {
            var messages = errors
                .EnumerateArray()
                .Where(static error => error.ValueKind == JsonValueKind.String)
                .Select(static error => error.GetString())
                .Where(static error => !string.IsNullOrWhiteSpace(error));

            return string.Join("; ", messages);
        }

        if (errors.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var keyedMessages = errors
            .EnumerateObject()
            .SelectMany(static property => property.Value.EnumerateArray())
            .Where(static error => error.ValueKind == JsonValueKind.String)
            .Select(static error => error.GetString())
            .Where(static error => !string.IsNullOrWhiteSpace(error));

        return string.Join("; ", keyedMessages);
    }
}
