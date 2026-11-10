using ST.SharedKernel.Dtos.Common;

namespace ST.Application.Features.ControlPlane.Tenants.Contracts;

public interface ITenantDatabaseReadiness
{
    Task<AppResponse<bool>> VerifyAsync(
        Guid tenantId,
        string connectionString,
        string provider,
        CancellationToken cancellationToken,
        bool requireIsolated = true);
}
