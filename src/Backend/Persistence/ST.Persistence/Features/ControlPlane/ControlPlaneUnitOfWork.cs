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
using ST.Domain.Features.ControlPlane.Modules.Entities;
using ST.Domain.Features.ControlPlane.Support.Entities;

namespace ST.Persistence.Features.ControlPlane;

public class ControlPlaneUnitOfWork : IControlPlaneUnitOfWork
{
    private readonly ControlPlaneDBContext _context;

    public IRepository<SupportGrant> SupportGrants => new Repository<SupportGrant>(_context);
    public IRepository<SupportAudit> SupportAudit => new Repository<SupportAudit>(_context);
    public IRepository<ModuleDefinition> Modules => new Repository<ModuleDefinition>(_context);
    public IRepository<ModuleCatalogueEntry> Catalogue => new Repository<ModuleCatalogueEntry>(_context);
    public IRepository<ModuleFeatureFlag> FeatureFlags => new Repository<ModuleFeatureFlag>(_context);
    public IRepository<TenantModule> TenantModules => new Repository<TenantModule>(_context);
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
