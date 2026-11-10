using Microsoft.EntityFrameworkCore;
using ST.Domain.Features.IAM.Permissions.Contracts.Repositories;
using ST.Domain.Features.IAM.Permissions.Entities;
using ST.Persistence.Common.Repositories;
using ST.Persistence.Features.IAM.DataContext;

namespace ST.Persistence.Features.IAM.Permissions.Repositories;

public sealed class IamPermissionRepository(IamDBContext context)
    : Repository<Permission>(context), IPermissionRepository
{
    public Task<bool> HasUsedKeyAsync(string key, CancellationToken cancellationToken)
    {
        var tenantId = context.CurrentTenantId;
        return context.Permissions.IgnoreQueryFilters().AnyAsync(permission =>
            permission.TenantId == tenantId && permission.Key == key, cancellationToken);
    }
}
