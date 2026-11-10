using ST.Infrastructure.Contracts.Interfaces;
using ST.Infrastructure.Logging;
using ST.Infrastructure.Configuration;
using ST.SharedKernel.Dtos.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ST.Infrastructure.Contracts.Implementations.Common;

internal sealed class ApiService : IApiService
{
    private readonly TimeProvider _timeProvider;
    private readonly HttpClient _httpClient;
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<ApiService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiService(HttpClient httpClient, IOptions<ApiSettings> appSettings, ILogger<ApiService> logger, IHttpContextAccessor httpContextAccessor, JsonSerializerOptions jsonOptions, TimeProvider timeProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _timeProvider = timeProvider;
        _apiSettings = appSettings.Value ?? throw new ArgumentNullException(nameof(appSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

        // CRITICAL: Validate BaseAddress is set
        if (_httpClient.BaseAddress == null)
        {
            var baseUrl = _apiSettings.BaseUrl ?? "https://localhost:7291/";
            HttpClientLogDefinitions.LogBaseAddressFallback(_logger, baseUrl);
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        HttpClientLogDefinitions.LogApiServiceDebug(_logger, _httpClient.BaseAddress);
        _jsonOptions = jsonOptions ?? throw new ArgumentNullException(nameof(jsonOptions));
    }

    public async Task<AppResponse<TResponse?>> DeleteAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
    {
        var responseMessage = string.Empty;
        try
        {

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));
            }

            SetAuthorizationHeader();

            var apiResponse = await _httpClient.DeleteAsync(endpoint, cancellationToken).ConfigureAwait(false);
            if (!apiResponse.IsSuccessStatusCode)
            {
                var content = await apiResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var errorMessage = ExtractErrorMessage(content);

                // Provide context-appropriate error messages
                var contextualError = GetErrorMessage(apiResponse.StatusCode, errorMessage, apiResponse.ReasonPhrase);

                HttpClientLogDefinitions.LogExternalApiWarning(_logger, "DELETE", endpoint, (int)apiResponse.StatusCode);

                return AppResponses.Failure<TResponse?>(contextualError);

            }

            var response = await apiResponse.Content.ReadFromJsonAsync<AppResponse<TResponse>>(cancellationToken: cancellationToken).ConfigureAwait(false);
            if (response == null)
            {
                responseMessage = "Response content is null";
                return AppResponses.Failure<TResponse?>("Response is null");
            }

            responseMessage = response.Message ?? "Resource deleted successfully";
            return AppResponses.Success(response.Message ?? "Resource deleted successfully", response.Data)!;

        }
        catch (Exception ex)
        {
            HttpClientLogDefinitions.LogExternalApiError(_logger, "DELETE", endpoint, ex);
            throw;
        }
    }

    public async Task<AppResponse<TResponse?>> GetAsync<TRequest, TResponse>(string endpoint, TRequest? request = default, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));
            }

            SetAuthorizationHeader();

            var apiResponse = await _httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
            if (!apiResponse.IsSuccessStatusCode)
            {
                var content = await apiResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var errorMessage = ExtractErrorMessage(content);

                var contextualError = GetErrorMessage(apiResponse.StatusCode, errorMessage, apiResponse.ReasonPhrase);

                HttpClientLogDefinitions.LogExternalApiWarning(_logger, "GET", endpoint, (int)apiResponse.StatusCode);

                return AppResponses.Failure<TResponse?>(contextualError);
            }


            var responseContent = await apiResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var response = JsonSerializer.Deserialize<AppResponse<TResponse>>(responseContent, _jsonOptions);

            //var response = await apiResponse.Content.ReadFromJsonAsync<AppResponse<TResponse>>();

            return response == null
                ? AppResponses.Failure<TResponse?>("Response content is null")
                : AppResponses.Success<TResponse?>(response.Message ?? "Operation completed.", response.Data);
        }
        catch (Exception ex)
        {
            HttpClientLogDefinitions.LogExternalApiError(_logger, "GET", endpoint, ex);
            throw;
        }
    }

    public async Task<AppResponse<TResponse?>> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
    {
        string responseMessage = string.Empty;
        try
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));
            }

            SetAuthorizationHeader();

            var apiResponse = await _httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
            if (!apiResponse.IsSuccessStatusCode)
            {
                var errorContent = await apiResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var errorMessage = ExtractErrorMessage(errorContent);

                var contextualError = GetErrorMessage(apiResponse.StatusCode, errorMessage, apiResponse.ReasonPhrase);

                HttpClientLogDefinitions.LogExternalApiWarning(_logger, "GET", endpoint, (int)apiResponse.StatusCode);

                return AppResponses.Failure<TResponse?>(contextualError);
            }

            var responseContent = await apiResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var response = JsonSerializer.Deserialize<AppResponse<TResponse>>(responseContent, _jsonOptions);

            if (response == null)
            {
                responseMessage = "Response content is null";
                return AppResponses.Failure<TResponse?>("Response content is null");
            }

            responseMessage = response.Message ?? "Records fetched successfully";
            return AppResponses.Success(response.Message ?? "Records fetched successfully", response.Data)!;
        }
        catch (Exception ex)
        {
            HttpClientLogDefinitions.LogExternalApiError(_logger, "GET", endpoint, ex);
            return AppResponses.Failure<TResponse?>(responseMessage);
        }
    }

    public async Task<AppResponse<TResponse?>> PostAsync<TRequest, TResponse>(string endpoint, TRequest? request, CancellationToken cancellationToken = default)
    {
        string responseMessage = string.Empty;
        string sessionId = string.Empty;

        try
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));
            }

            // Ensure BaseAddress is set
            if (_httpClient.BaseAddress == null)
            {
                var baseUrl = _apiSettings.BaseUrl ?? "https://localhost:7291/";
                _httpClient.BaseAddress = new Uri(baseUrl);
                HttpClientLogDefinitions.LogBaseAddressFallback(_logger, baseUrl);
            }

            var cleanEndpoint = endpoint.TrimStart('/');

            SetAuthorizationHeader();

            // Encrypt the request payload
            var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
            using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var apiResponse = await _httpClient.PostAsync(endpoint, content, cancellationToken).ConfigureAwait(false);

            // Capture X-Session-Id header
            if (apiResponse.Headers.TryGetValues("X-Session-Id", out var sessionIdValues))
            {
                sessionId = sessionIdValues.FirstOrDefault() ?? string.Empty;

            }

            if (!apiResponse.IsSuccessStatusCode)
            {
                var errorContent = await apiResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var errorMessage = ExtractErrorMessage(errorContent);

                var contextualError = GetErrorMessage(apiResponse.StatusCode, errorMessage, apiResponse.ReasonPhrase);

                HttpClientLogDefinitions.LogExternalApiWarning(_logger, "POST", endpoint, (int)apiResponse.StatusCode);

                return AppResponses.Failure<TResponse?>(contextualError);
            }

            var responseContent = await apiResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var response = JsonSerializer.Deserialize<AppResponse<TResponse>>(responseContent, _jsonOptions);

            if (response == null)
            {
                responseMessage = "Response content is null";
                return AppResponses.Failure<TResponse?>("Response content is null");
            }

            response = response with { SessionId = sessionId };

            return AppResponses.Success(response.Message ?? "Resource created successfully", response.Data)!;
        }
        catch (Exception ex)
        {
            HttpClientLogDefinitions.LogExternalApiError(_logger, "POST", endpoint, ex);
            return AppResponses.Failure<TResponse?>("An error occurred while processing your request.");
        }
    }

    public async Task<AppResponse<TResponse?>> PutAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            SetAuthorizationHeader();

            // Encrypt the request payload
            var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
            using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var apiResponse = await _httpClient.PutAsync(endpoint, content, cancellationToken).ConfigureAwait(false);
            if (!apiResponse.IsSuccessStatusCode)
            {
                var errorContent = await apiResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var errorMessage = ExtractErrorMessage(errorContent);

                var contextualError = GetErrorMessage(apiResponse.StatusCode, errorMessage, apiResponse.ReasonPhrase);

                HttpClientLogDefinitions.LogExternalApiWarning(_logger, "PUT", endpoint, (int)apiResponse.StatusCode);

                return AppResponses.Failure<TResponse?>(contextualError);
            }

            var responseContent = await apiResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var response = JsonSerializer.Deserialize<AppResponse<TResponse>>(responseContent, _jsonOptions);

            return response switch
            {
                null => AppResponses.Failure<TResponse?>("Response content is null"),

                { IsSuccess: false } or { Data: null }
                    => AppResponses.Failure<TResponse?>(response.Message ?? "Update operation failed"),

                _ => AppResponses.Success<TResponse?>(
                        response.Message ?? "Resource updated successfully",
                        response.Data)
            };

        }
        catch (Exception ex)
        {
            HttpClientLogDefinitions.LogExternalApiError(_logger, "PUT", endpoint, ex);
            throw;
        }
    }

    public async Task<AppResponse<PagedResponse<TResponse, TCursor>>> GetPagedAsync<TResponse, TCursor>(string endpoint, CancellationToken cancellationToken = default)
    {
        string content = string.Empty;
        try
        {
            SetAuthorizationHeader();

            var apiResponse = await _httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
            if (!apiResponse.IsSuccessStatusCode)
            {
                content = await apiResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var errorMessage = ExtractErrorMessage(content);

                var contextualError = GetErrorMessage(apiResponse.StatusCode, errorMessage, apiResponse.ReasonPhrase);

                HttpClientLogDefinitions.LogExternalApiWarning(_logger, "GET", endpoint, (int)apiResponse.StatusCode);

                return AppResponses.Failure<PagedResponse<TResponse, TCursor>>(contextualError);
            }

            content = await apiResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var response = JsonSerializer.Deserialize<AppResponse<PagedResponse<TResponse, TCursor>>>(content, _jsonOptions);

            return response ?? AppResponses.Failure<PagedResponse<TResponse, TCursor>>("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            HttpClientLogDefinitions.LogExternalApiError(_logger, "GET", endpoint, ex);
            return AppResponses.Failure<PagedResponse<TResponse, TCursor>>("The external service could not complete the request. Please try again.");
        }

    }

    public async Task<AppResponse<PagedResponse<TResponse, TCursor>>> GetPagedAsync<TRequest, TResponse, TCursor>(string endpoint, TRequest? request, CancellationToken cancellationToken = default)
    {
        try
        {
            SetAuthorizationHeader();

            var apiResponse = await _httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
            if (!apiResponse.IsSuccessStatusCode)
            {
                var errorContent = await apiResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var errorMessage = ExtractErrorMessage(errorContent);

                var contextualError = GetErrorMessage(apiResponse.StatusCode, errorMessage, apiResponse.ReasonPhrase);

                HttpClientLogDefinitions.LogExternalApiWarning(_logger, "GET", endpoint, (int)apiResponse.StatusCode);

                return AppResponses.Failure<PagedResponse<TResponse, TCursor>>(contextualError);
            }

            var responseContent = await apiResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var response = JsonSerializer.Deserialize<AppResponse<PagedResponse<TResponse, TCursor>>>(responseContent, _jsonOptions);

            //var response = JsonSerializer.Deserialize<AppResponse<PagedResult<TResponse>>>(content, new JsonSerializerOptions{PropertyNameCaseInsensitive = true});

            return response ?? AppResponses.Failure<PagedResponse<TResponse, TCursor>>("Failed to deserialize response");

        }
        catch (Exception ex)
        {
            HttpClientLogDefinitions.LogExternalApiError(_logger, "GET", endpoint, ex);
            return AppResponses.Failure<PagedResponse<TResponse, TCursor>>("The external service could not complete the request. Please try again.");
        }
    }

    public async Task<AppResponse<TResponse>> PatchAsync<TRequest, TResponse>(string endpoint, TRequest kpi, CancellationToken cancellationToken = default)
    {
        string responseMessage = string.Empty;
        try
        {
            SetAuthorizationHeader();

            var apiResponse = await _httpClient.PatchAsJsonAsync(endpoint, kpi, cancellationToken).ConfigureAwait(false);
            if (!apiResponse.IsSuccessStatusCode)
            {
                var content = await apiResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var errorMessage = ExtractErrorMessage(content);

                var contextualError = GetErrorMessage(apiResponse.StatusCode, errorMessage, apiResponse.ReasonPhrase);

                HttpClientLogDefinitions.LogExternalApiWarning(_logger, "PATCH", endpoint, (int)apiResponse.StatusCode);
                return AppResponses.Failure<TResponse>(contextualError);

            }
            var response = await apiResponse.Content.ReadFromJsonAsync<AppResponse<TResponse>>(cancellationToken: cancellationToken).ConfigureAwait(false);
            if (response == null)
            {
                responseMessage = "Response content is null";
                return AppResponses.Failure<TResponse>("Response content is null");

            }
            if (!response.IsSuccess || response.Data == null)
            {
                responseMessage = response.Message ?? "Patch operation failed";
                return AppResponses.Failure<TResponse>(response.Message ?? "Patch operation failed");
            }


            responseMessage = response.Message ?? "Patch operation successful";
            return AppResponses.Success(response.Message ?? "Patch operation successful", response.Data);

        }
        catch (Exception ex)
        {

            HttpClientLogDefinitions.LogExternalApiError(_logger, "PATCH", endpoint, ex);
            return AppResponses.Failure<TResponse>(responseMessage);
        }
    }



    private void SetAuthorizationHeader()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        var token = httpContext.Request.Cookies["auth_token"];
        if (string.IsNullOrWhiteSpace(token))
        {
            token = httpContext.Session.GetString("auth_token");
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            var (isValid, expiry) = IsTokenValid(token);
            if (!isValid)
            {
                HttpClientLogDefinitions.LogTokenExpired(_logger, expiry ?? _timeProvider.GetUtcNow().UtcDateTime);
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        // ALWAYS include session ID if available
        _httpClient.DefaultRequestHeaders.Remove("X-Session-Id");
        var sessionId = httpContext.Request.Cookies["session_id"];
        if (!string.IsNullOrEmpty(sessionId))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Session-Id", sessionId);
        }

    }

    private (bool, DateTime?) IsTokenValid(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(token))
            {
                HttpClientLogDefinitions.LogExternalApiError(_logger, "Token Validation", "Invalid token format", new InvalidOperationException());
                return (false, null);
            }

            var jwtToken = tokenHandler.ReadJwtToken(token);
            var expiry = jwtToken.ValidTo;
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            if (expiry <= now)
            {
                HttpClientLogDefinitions.LogTokenExpired(_logger, expiry);
                return (false, expiry);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            HttpClientLogDefinitions.LogExternalApiError(_logger, "Token Validation", "N/A", ex);
            return (false, null);
        }
    }

    private string? ExtractErrorMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        // Try parsing as plaintext JSON
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (TryExtractMessage(doc.RootElement, out var message) && !string.IsNullOrEmpty(message))
            {
                return message;
            }
        }
        catch (JsonException ex)
        {
            HttpClientLogDefinitions.LogNonJsonErrorContent(_logger, ex);
            // Not valid JSON — may be encrypted or plain text error;
        }

        // Fallback to raw content
        // Only return raw content if it's short and safe (avoid leaking sensitive/long data)
        return content.Length <= 200 ? content : "An error occurred.";
    }

    private static bool TryExtractMessage(JsonElement root, out string? message)
    {
        message = null;

        if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
        {
            message = m.GetString();
            return !string.IsNullOrEmpty(message);
        }

        if (root.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
        {
            message = t.GetString();
            return !string.IsNullOrEmpty(message);
        }

        if (root.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.String)
        {
            message = e.GetString();
            return !string.IsNullOrEmpty(message);
        }

        if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
        {
            var errorList = new List<string>();
            foreach (var err in errors.EnumerateArray())
            {
                if (err.ValueKind == JsonValueKind.String)
                {
                    var str = err.GetString();
                    if (!string.IsNullOrEmpty(str))
                        errorList.Add(str);
                }
            }

            if (errorList.Any())
            {
                message = string.Join("; ", errorList);
                return true;
            }
        }

        return false;
    }

    private static string GetErrorMessage(HttpStatusCode statusCode, string? extractedErrorMessage, string? reasonPhrase)
    {
        // Define default messages for each status code
        var defaultMessage = statusCode switch
        {
            HttpStatusCode.Unauthorized => "Authentication required. Please sign in again.",
            HttpStatusCode.Forbidden => "You don't have permission to perform this action.",
            HttpStatusCode.NotFound => "The requested resource was not found.",
            HttpStatusCode.BadRequest => "Invalid request data.",
            HttpStatusCode.InternalServerError => "Server error occurred. Please try again later.",
            _ => "Request failed. Please try again."
        };

        // Return the most specific available message
        return !string.IsNullOrWhiteSpace(extractedErrorMessage)
            ? extractedErrorMessage
            : !string.IsNullOrWhiteSpace(reasonPhrase) ? reasonPhrase : defaultMessage;
    }

}
