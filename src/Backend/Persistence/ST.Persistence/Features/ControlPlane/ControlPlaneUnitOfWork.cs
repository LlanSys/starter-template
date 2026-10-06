using ST.Domain.Features.ControlPlane.Auditing.Contracts.Repositories;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Contracts.Repositories;
using ST.Persistence.Features.ControlPlane.DataContext;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ST.Persistence.Features.ControlPlane;

public class ControlPlaneUnitOfWork : IControlPlaneUnitOfWork
{
    private readonly ControlPlaneDBContext _context;

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
