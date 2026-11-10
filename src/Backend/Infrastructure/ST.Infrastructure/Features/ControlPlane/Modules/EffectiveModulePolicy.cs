using ST.Application.Features.ControlPlane.Modules;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Features.IAM.Contracts;
namespace ST.Infrastructure.Features.ControlPlane.Modules;
internal sealed class EffectiveModulePolicy(IControlPlaneUnitOfWork unit, ICurrentTenantProvider tenantProvider, IIamUnitOfWork iam, TimeProvider timeProvider) : IEffectiveModulePolicy
{
    private Guid? _scopeTenant;
    private Task<EffectiveModuleAccess>? _pending;
    public Task<EffectiveModuleAccess> GetAsync(CancellationToken cancellationToken = default)
    {
        var id=tenantProvider.TenantId;
        if(_pending is null || _scopeTenant != id) { _scopeTenant=id;_pending=LoadAsync(id,cancellationToken); }
        return _pending;
    }
    private async Task<EffectiveModuleAccess> LoadAsync(Guid tenantId,CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var definitions = await unit.Modules.ListAsync(ct: cancellationToken).ConfigureAwait(false);
        var catalogue = await unit.Catalogue.ListAsync(ct: cancellationToken).ConfigureAwait(false);
        var tenant = await unit.Tenants.FirstOrDefaultAsync(query => query.Where(x => x.Id == tenantId).Select(x => new { x.Status, Assignments = x.Modules.Where(m => m.IsActive && (m.ExpiresAt == null || m.ExpiresAt > now)).Select(m => m.ModuleKey).ToArray() }), cancellationToken).ConfigureAwait(false);
        var flags = await unit.FeatureFlags.ListAsync(query => query.Where(x => x.ScopeTenantId == Guid.Empty || x.ScopeTenantId == tenantId), cancellationToken).ConfigureAwait(false);
        var enabledFlags = flags.Where(x => x.ScopeTenantId == Guid.Empty).Where(definition => (flags.FirstOrDefault(x => x.ScopeTenantId == tenantId && x.ModuleKey == definition.ModuleKey && x.Key == definition.Key)?.IsEnabled ?? definition.IsEnabled)).Select(x => $"{x.ModuleKey}:{x.Key}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidates = definitions.Where(x => x.IsActive && !x.IsRetired && ModuleImplementations.Contains(x.Key)
            && !flags.Any(f => f.ScopeTenantId == Guid.Empty && string.Equals(f.ModuleKey,x.Key,StringComparison.OrdinalIgnoreCase) && f.Key == "enabled" && !enabledFlags.Contains($"{f.ModuleKey}:enabled")))
            .Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (tenant?.Status != TenantStatus.Active) candidates.Clear();
        else candidates.IntersectWith(tenant.Assignments);
        var graph = definitions.ToDictionary(x => x.Key, x => x.DependencyKeys, StringComparer.OrdinalIgnoreCase);
        var privatePermissions = await iam.PermissionRepository.ListAsync(ct: cancellationToken).ConfigureAwait(false);
        return new EffectiveModuleAccess { PrivatePermissionModules = privatePermissions.ToDictionary(x => x.Key, x => x.ModuleKey, StringComparer.OrdinalIgnoreCase), InactivePrivatePermissions = privatePermissions.Where(x => !x.IsActive).Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase), Modules = ModuleDependencyGraph.Resolve(graph, candidates), Catalogue = catalogue, EnabledFlags = enabledFlags };
    }
}
