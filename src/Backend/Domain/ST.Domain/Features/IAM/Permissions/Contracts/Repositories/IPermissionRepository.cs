using ST.Domain.Features.IAM.Permissions.Entities;
using ST.Domain.Shared.Contracts.Repositories;

namespace ST.Domain.Features.IAM.Permissions.Contracts.Repositories;

public interface IPermissionRepository : IRepository<Permission>
{
    Task<bool> HasUsedKeyAsync(string key, CancellationToken cancellationToken);
}
