using ST.Domain.Features.ControlPlane.Auditing.Contracts.Repositories;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Contracts.Repositories;
using ST.Persistence.Features.ControlPlane.DataContext;
using System;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Shared.Contracts.Repositories;
using ST.Persistence.Common.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace ST.Persistence.Features.ControlPlane;

public class ControlPlaneUnitOfWork : IControlPlaneUnitOfWork
{
    private readonly ControlPlaneDBContext _context;

    public ST.Domain.Shared.Contracts.Repositories.IRepository<ST.Domain.Features.ControlPlane.Support.Entities.SupportGrant> SupportGrants => new ST.Persistence.Common.Repositories.Repository<ST.Domain.Features.ControlPlane.Support.Entities.SupportGrant>(_context);
    public ST.Domain.Shared.Contracts.Repositories.IRepository<ST.Domain.Features.ControlPlane.Support.Entities.SupportAudit> SupportAudit => new ST.Persistence.Common.Repositories.Repository<ST.Domain.Features.ControlPlane.Support.Entities.SupportAudit>(_context);
    public ST.Domain.Shared.Contracts.Repositories.IRepository<ST.Domain.Features.ControlPlane.Modules.Entities.ModuleDefinition> Modules => new ST.Persistence.Common.Repositories.Repository<ST.Domain.Features.ControlPlane.Modules.Entities.ModuleDefinition>(_context);
    public ST.Domain.Shared.Contracts.Repositories.IRepository<ST.Domain.Features.ControlPlane.Modules.Entities.ModuleCatalogueEntry> Catalogue => new ST.Persistence.Common.Repositories.Repository<ST.Domain.Features.ControlPlane.Modules.Entities.ModuleCatalogueEntry>(_context);
    public ST.Domain.Shared.Contracts.Repositories.IRepository<ST.Domain.Features.ControlPlane.Modules.Entities.ModuleFeatureFlag> FeatureFlags => new ST.Persistence.Common.Repositories.Repository<ST.Domain.Features.ControlPlane.Modules.Entities.ModuleFeatureFlag>(_context);
    public ST.Domain.Shared.Contracts.Repositories.IRepository<ST.Domain.Features.ControlPlane.Tenants.Entities.TenantModule> TenantModules => new ST.Persistence.Common.Repositories.Repository<ST.Domain.Features.ControlPlane.Tenants.Entities.TenantModule>(_context);
    public IRepository<TenantAdministratorInvitation> AdministratorInvitations => new Repository<TenantAdministratorInvitation>(_context);
    public ITenantRepository Tenants { get; }
    public IDeploymentStampRepository DeploymentStamps { get; }
    public IImpersonationRecordRepository ImpersonationRecords { get; }

    public ControlPlaneUnitOfWork(
        ControlPlaneDBContext context,
        ITenantRepository tenants,
        IDeploymentStampRepository deploymentStamps,
        IImpersonationRecordRepository impersonationRecords)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        Tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        DeploymentStamps = deploymentStamps ?? throw new ArgumentNullException(nameof(deploymentStamps));
        ImpersonationRecords = impersonationRecords ?? throw new ArgumentNullException(nameof(impersonationRecords));
    }

    public async Task<int> CompleteAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
