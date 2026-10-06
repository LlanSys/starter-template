using ST.Domain.Features.ControlPlane.Tenants.Contracts.Repositories;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Persistence.Common;
using ST.Persistence.Common.Repositories;
using ST.Persistence.Features.ControlPlane.DataContext;
using Microsoft.EntityFrameworkCore;

namespace ST.Persistence.Features.ControlPlane.Tenants.Repositories;

public class DeploymentStampRepository : Repository<DeploymentStamp>, IDeploymentStampRepository
{
    public DeploymentStampRepository(ControlPlaneDBContext context) : base(context)
    {
    }
}
