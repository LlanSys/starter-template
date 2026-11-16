using Microsoft.EntityFrameworkCore;
using ST.Domain.Features.IAM.Contracts;
using ST.Persistence.Features.ControlPlane.DataContext;

namespace ST.Infrastructure.Features.IAM.Authorization;

internal sealed class TenantAccountLimitProvider(ControlPlaneDBContext context) : ITenantAccountLimitProvider
{
    public Task<int?> GetLimitAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return context.Tenants.AsNoTracking()
            .Where(tenant => tenant.Id == tenantId)
            .Select(tenant => (int?)tenant.MaxUsers)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
