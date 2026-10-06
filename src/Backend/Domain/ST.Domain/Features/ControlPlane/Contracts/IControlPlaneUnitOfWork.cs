using ST.Domain.Features.ControlPlane.Auditing.Contracts.Repositories;
using ST.Domain.Features.ControlPlane.Tenants.Contracts.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ST.Domain.Features.ControlPlane.Contracts;

public interface IControlPlaneUnitOfWork : IDisposable, IAsyncDisposable
{
    ITenantRepository Tenants { get; }
    IDeploymentStampRepository DeploymentStamps { get; }
    IImpersonationRecordRepository ImpersonationRecords { get; }

    Task<int> CompleteAsync(CancellationToken cancellationToken = default);
}
