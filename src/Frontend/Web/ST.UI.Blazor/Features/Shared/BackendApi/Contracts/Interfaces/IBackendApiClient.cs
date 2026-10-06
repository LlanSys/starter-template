using ST.SharedKernel.Dtos.Common;

namespace ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;

internal interface IBackendApiClient
{
    Task<AppResponse<T>> SendAsync<T>(
        HttpMethod method,
        string endpoint,
        object? request = null,
        bool requiresAuthentication = true,
        string? unavailableMessage = null,
        string? timeoutMessage = null,
        CancellationToken cancellationToken = default);

    Task<AppResponse<T>> SendMultipartAsync<T>(
        string endpoint,
        MultipartFormDataContent content,
        bool requiresAuthentication = true,
        string? unavailableMessage = null,
        string? timeoutMessage = null,
        CancellationToken cancellationToken = default);

    Task<AppResponse<FileContentResponse>> SendFileAsync(
        string endpoint,
        bool requiresAuthentication = true,
        string? unavailableMessage = null,
        string? timeoutMessage = null,
        CancellationToken cancellationToken = default);
}
