using ST.Domain.Features.ControlPlane.Auditing.Contracts.Repositories;
using ST.Domain.Features.ControlPlane.Tenants.Contracts.Repositories;
using System;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Shared.Contracts.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace ST.Domain.Features.ControlPlane.Contracts;

public interface IControlPlaneUnitOfWork : IDisposable, IAsyncDisposable
{
    ST.Domain.Shared.Contracts.Repositories.IRepository<ST.Domain.Features.ControlPlane.Support.Entities.SupportGrant> SupportGrants { get; }
    ST.Domain.Shared.Contracts.Repositories.IRepository<ST.Domain.Features.ControlPlane.Support.Entities.SupportAudit> SupportAudit { get; }
    ST.Domain.Shared.Contracts.Repositories.IRepository<ST.Domain.Features.ControlPlane.Modules.Entities.ModuleCatalogueEntry> Catalogue { get; }
    ST.Domain.Shared.Contracts.Repositories.IRepository<ST.Domain.Features.ControlPlane.Modules.Entities.ModuleFeatureFlag> FeatureFlags { get; }
    ST.Domain.Shared.Contracts.Repositories.IRepository<ST.Domain.Features.ControlPlane.Modules.Entities.ModuleDefinition> Modules { get; }
    ST.Domain.Shared.Contracts.Repositories.IRepository<ST.Domain.Features.ControlPlane.Tenants.Entities.TenantModule> TenantModules { get; }
    IRepository<TenantAdministratorInvitation> AdministratorInvitations { get; }
    ITenantRepository Tenants { get; }
    IDeploymentStampRepository DeploymentStamps { get; }
    IImpersonationRecordRepository ImpersonationRecords { get; }

    Task<int> CompleteAsync(CancellationToken cancellationToken = default);
}
