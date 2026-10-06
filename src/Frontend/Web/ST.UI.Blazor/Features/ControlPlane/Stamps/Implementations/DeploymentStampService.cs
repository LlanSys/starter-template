using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Stamps.Dtos;
using ST.UI.Blazor.Features.ControlPlane.Stamps.Contracts;
using ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;

namespace ST.UI.Blazor.Features.ControlPlane.Stamps.Implementations;

internal class DeploymentStampService(IBackendApiClient client) : IDeploymentStampService
{
    private const string BasePath = "api/v1/control-plane/stamps";

    public async Task<AppResponse<List<DeploymentStampResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await client.SendAsync<List<DeploymentStampResponse>>(
            HttpMethod.Get,
            BasePath,
            requiresAuthentication: true,
            unavailableMessage: "Unable to retrieve deployment stamps.",
            cancellationToken: cancellationToken);
    }

    public async Task<AppResponse<DeploymentStampResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await client.SendAsync<DeploymentStampResponse>(
            HttpMethod.Get,
            $"{BasePath}/{id}",
            requiresAuthentication: true,
            unavailableMessage: "Unable to retrieve deployment stamp.",
            cancellationToken: cancellationToken);
    }

    public async Task<AppResponse<DeploymentStampResponse>> CreateAsync(CreateDeploymentStampRequest request, CancellationToken cancellationToken = default)
    {
        return await client.SendAsync<DeploymentStampResponse>(
            HttpMethod.Post,
            BasePath,
            request: request,
            requiresAuthentication: true,
            unavailableMessage: "Unable to create deployment stamp.",
            cancellationToken: cancellationToken);
    }

    public async Task<AppResponse<DeploymentStampResponse>> UpdateAsync(Guid id, UpdateDeploymentStampRequest request, CancellationToken cancellationToken = default)
    {
        return await client.SendAsync<DeploymentStampResponse>(
            HttpMethod.Put,
            $"{BasePath}/{id}",
            request: request,
            requiresAuthentication: true,
            unavailableMessage: "Unable to update deployment stamp.",
            cancellationToken: cancellationToken);
    }
}
