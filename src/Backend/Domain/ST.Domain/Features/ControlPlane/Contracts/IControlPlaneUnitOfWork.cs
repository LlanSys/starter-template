using ST.Domain.Features.ControlPlane.Auditing.Contracts.Repositories;
using ST.Domain.Features.ControlPlane.Tenants.Contracts.Repositories;
using System;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Shared.Contracts.Repositories;
using System.Threading;
using System.Threading.Tasks;
using ST.Domain.Features.ControlPlane.Modules.Entities;
using ST.Domain.Features.ControlPlane.Support.Entities;

namespace ST.Domain.Features.ControlPlane.Contracts;

public interface IControlPlaneUnitOfWork : IDisposable, IAsyncDisposable
{
    IRepository<SupportGrant> SupportGrants { get; }
    IRepository<SupportAudit> SupportAudit { get; }
    IRepository<ModuleCatalogueEntry> Catalogue { get; }
    IRepository<ModuleFeatureFlag> FeatureFlags { get; }
    IRepository<ModuleDefinition> Modules { get; }
    IRepository<TenantModule> TenantModules { get; }
    IRepository<TenantAdministratorInvitation> AdministratorInvitations { get; }
    ITenantRepository Tenants { get; }
    IDeploymentStampRepository DeploymentStamps { get; }
    IImpersonationRecordRepository ImpersonationRecords { get; }

    Task<int> CompleteAsync(CancellationToken cancellationToken = default);
}
