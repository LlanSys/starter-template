using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Shared.Contracts.Repositories;

namespace ST.Domain.Features.ControlPlane.Tenants.Contracts.Repositories;

public interface ITenantRepository : IRepository<Tenant>
{
}
