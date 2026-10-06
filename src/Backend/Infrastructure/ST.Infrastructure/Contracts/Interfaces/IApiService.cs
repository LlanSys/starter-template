using ST.SharedKernel.Dtos.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Infrastructure.Contracts.Interfaces;

internal interface IApiService
{
    Task<AppResponse<TResponse?>> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default);
    Task<AppResponse<PagedResponse<TResponse, TCursor>>> GetPagedAsync<TResponse, TCursor>(string endpoint, CancellationToken cancellationToken = default);
    Task<AppResponse<PagedResponse<TResponse, TCursor>>> GetPagedAsync<TRequest, TResponse, TCursor>(string endpoint, TRequest? request, CancellationToken cancellationToken = default);
    Task<AppResponse<TResponse?>> GetAsync<TRequest, TResponse>(string endpoint, TRequest? request, CancellationToken cancellationToken = default);
    Task<AppResponse<TResponse?>> PostAsync<TRequest, TResponse>(string endpoint, TRequest? request, CancellationToken cancellationToken = default);
    Task<AppResponse<TResponse?>> PutAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default);
    Task<AppResponse<TResponse?>> DeleteAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default);
    Task<AppResponse<TResponse>> PatchAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default);
}
