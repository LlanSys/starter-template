using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ST.SharedKernel.Features.ControlPlane.Auditing.Dtos;
using ST.SharedKernel.Dtos.Common;
using ST.UI.Blazor.Features.ControlPlane.Auditing.Contracts;
using ST.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;

namespace ST.UI.Blazor.Features.ControlPlane.Auditing.Implementations;

internal class AuditService(IBackendApiClient client) : IAuditService
{
    private const string BasePath = "api/v1/control-plane/audit/impersonate";

    public async Task<AppResponse<ImpersonationRecordResponse>> StartImpersonationAsync(StartImpersonationRequest request, CancellationToken cancellationToken = default)
    {
        return await client.SendAsync<ImpersonationRecordResponse>(
            HttpMethod.Post,
            $"{BasePath}/start",
            request: request,
            requiresAuthentication: true,
            unavailableMessage: "Unable to start impersonation.",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppResponse<bool>> EndImpersonationAsync(Guid impersonationRecordId, CancellationToken cancellationToken = default)
    {
        return await client.SendAsync<bool>(
            HttpMethod.Post,
            $"{BasePath}/end",
            request: new EndImpersonationRequest(impersonationRecordId),
            requiresAuthentication: true,
            unavailableMessage: "Unable to end impersonation.",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppResponse<IReadOnlyList<ImpersonationRecordDto>>> GetImpersonationRecordsAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        return await client.SendAsync<IReadOnlyList<ImpersonationRecordDto>>(
            HttpMethod.Get,
            $"{BasePath}/records?activeOnly={activeOnly}",
            requiresAuthentication: true,
            unavailableMessage: "Unable to get impersonation records.",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
