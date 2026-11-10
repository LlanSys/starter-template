using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using ST.UI.Blazor.Features.ControlPlane.Tenants.Contracts;
using ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;

namespace ST.UI.Blazor.Features.ControlPlane.Tenants.Implementations;

internal class TenantService(IBackendApiClient client) : ITenantService
{
    private const string BasePath = "api/v1/control-plane/tenants";

    public async Task<AppResponse<List<TenantResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await client.SendAsync<List<TenantResponse>>(
            HttpMethod.Get,
            BasePath,
            requiresAuthentication: true,
            unavailableMessage: "Unable to retrieve tenants.",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppResponse<TenantResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await client.SendAsync<TenantResponse>(
            HttpMethod.Get,
            $"{BasePath}/{id}",
            requiresAuthentication: true,
            unavailableMessage: "Unable to retrieve tenant.",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppResponse<TenantResponse>> CreateAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        return await client.SendAsync<TenantResponse>(
            HttpMethod.Post,
            BasePath,
            request: request,
            requiresAuthentication: true,
            unavailableMessage: "Unable to create tenant.",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppResponse<TenantResponse>> UpdateAsync(Guid id, UpdateTenantRequest request, CancellationToken cancellationToken = default)
    {
        return await client.SendAsync<TenantResponse>(
            HttpMethod.Put,
            $"{BasePath}/{id}",
            request: request,
            requiresAuthentication: true,
            unavailableMessage: "Unable to update tenant.",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppResponse<TenantResponse>> SuspendAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await client.SendAsync<TenantResponse>(
            HttpMethod.Post, // Fixed from Patch to Post as per API controller
            $"{BasePath}/{id}/suspend",
            requiresAuthentication: true,
            unavailableMessage: "Unable to suspend tenant.",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppResponse<TenantResponse>> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await client.SendAsync<TenantResponse>(
            HttpMethod.Post, // Fixed from Patch to Post as per API controller
            $"{BasePath}/{id}/activate",
            requiresAuthentication: true,
            unavailableMessage: "Unable to activate tenant.",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppResponse<TenantResponse>> ApproveAsync(Guid id, ApproveTenantRequest request, CancellationToken cancellationToken = default)
    {
        return await client.SendAsync<TenantResponse>(
            HttpMethod.Post,
            $"{BasePath}/{id}/approve",
            request: request,
            requiresAuthentication: true,
            unavailableMessage: "Unable to approve tenant.",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public Task<AppResponse<TenantResponse>> RetryProvisioningAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return client.SendAsync<TenantResponse>(
            HttpMethod.Post,
            $"{BasePath}/{id}/provisioning/retry",
            requiresAuthentication: true,
            unavailableMessage: "Unable to retry provisioning.",
            cancellationToken: cancellationToken);
    }

    public async Task<AppResponse<TenantResponse>> AddModuleAsync(Guid id, AddTenantModuleRequest request, CancellationToken cancellationToken = default)
    {
        return await client.SendAsync<TenantResponse>(
            HttpMethod.Post,
            $"{BasePath}/{id}/modules",
            request: request,
            requiresAuthentication: true,
            unavailableMessage: "Unable to add module.",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppResponse<TenantResponse>> RemoveModuleAsync(Guid id, string moduleKey, CancellationToken cancellationToken = default)
    {
        return await client.SendAsync<TenantResponse>(
            HttpMethod.Delete,
            $"{BasePath}/{id}/modules/{moduleKey}",
            requiresAuthentication: true,
            unavailableMessage: "Unable to remove module.",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
