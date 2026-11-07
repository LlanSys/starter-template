using MediatR;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Modules.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
namespace ST.Application.Features.ControlPlane.Modules;
internal sealed class SaveCatalogueEntryHandler(IControlPlaneUnitOfWork unit) : IRequestHandler<SaveCatalogueEntryCommand, AppResponse<CatalogueEntryResponse>>
{
    public async Task<AppResponse<CatalogueEntryResponse>> Handle(SaveCatalogueEntryCommand command, CancellationToken cancellationToken)
    {
        var moduleKey = command.ModuleKey.Trim().ToLowerInvariant();
        var modules = await unit.Modules.ListAsync(ct: cancellationToken).ConfigureAwait(false);
        var module = modules.FirstOrDefault(x => string.Equals(x.Key, moduleKey, StringComparison.OrdinalIgnoreCase));
        if (module is null || module.IsRetired) return AppResponses.Failure<CatalogueEntryResponse>("Select a registered module that is not retired.");
        var request = command.Request;
        if (!Enum.TryParse<ModuleCatalogueKind>(request.Kind, true, out var kind) || !Enum.IsDefined(kind)) return AppResponses.Failure<CatalogueEntryResponse>("Select a valid catalogue type.");
        var key = request.Key.Trim().ToLowerInvariant();
        if (key.StartsWith("control_plane.", StringComparison.OrdinalIgnoreCase) || request.Path.StartsWith("/control-panel", StringComparison.OrdinalIgnoreCase)) return AppResponses.Failure<CatalogueEntryResponse>("Platform authority cannot be declared in tenant module catalogues.");
        var all = await unit.Catalogue.ListAsync(ct: cancellationToken).ConfigureAwait(false);
        var existing = command.Id is { } id ? all.FirstOrDefault(x => x.Id == id && x.ModuleKey == moduleKey) : null;
        if (command.Id.HasValue && existing is null) return AppResponses.Failure<CatalogueEntryResponse>("Catalogue entry not found.");
        if (existing is not null && (existing.Key != key || existing.Kind != kind)) return AppResponses.Failure<CatalogueEntryResponse>("The catalogue key and type cannot change.");
        if (existing is not null && request.ExpectedVersion != existing.Version) return AppResponses.Failure<CatalogueEntryResponse>("This entry changed. Reload it before saving.");
        if (all.Any(x => x.Id != command.Id && string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase))) return AppResponses.Failure<CatalogueEntryResponse>("This catalogue key is already registered.");
        var permissions = (request.PermissionKeys ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToLowerInvariant()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (permissions.Any(permission => !all.Any(x => x.Kind == ModuleCatalogueKind.Permission && x.Key == permission && x.IsActive && (x.ModuleKey == moduleKey || module.DependencyKeys.Contains(x.ModuleKey, StringComparer.OrdinalIgnoreCase))))) return AppResponses.Failure<CatalogueEntryResponse>("Select active permissions owned by this module or one of its dependencies.");
        if (kind == ModuleCatalogueKind.Page)
        {
            if (!string.IsNullOrWhiteSpace(request.Path) && (!request.Path.StartsWith('/') || request.Path.StartsWith("//", StringComparison.Ordinal) || request.Path.Contains('?', StringComparison.Ordinal))) return AppResponses.Failure<CatalogueEntryResponse>("Use an application page path without a query string.");
            var boundOwner = ModulePageBindings.Owner(request.Path);
            if (boundOwner is not null && !string.Equals(boundOwner, moduleKey, StringComparison.OrdinalIgnoreCase)) return AppResponses.Failure<CatalogueEntryResponse>("This implemented page belongs to another module.");
            if (request.ParentKey is { Length: > 0 } parentKey)
            {
                var parent = all.FirstOrDefault(x => x.Key == parentKey && x.Kind == ModuleCatalogueKind.Page);
                if (parent is null || (parent.ModuleKey != moduleKey && !module.DependencyKeys.Contains(parent.ModuleKey, StringComparer.OrdinalIgnoreCase))) return AppResponses.Failure<CatalogueEntryResponse>("Parent must belong to this module or a dependency.");
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { key };
                for (var cursor = parent; cursor is not null; cursor = all.FirstOrDefault(x => x.Key == cursor.ParentKey))
                    if (!seen.Add(cursor.Key) || seen.Count > 64) return AppResponses.Failure<CatalogueEntryResponse>("The navigation hierarchy cannot contain a cycle or exceed 64 levels.");
            }
        }
        if (request.FeatureFlag is { Length: > 0 } flag && !await unit.FeatureFlags.AnyAsync(x => x.ModuleKey == moduleKey && x.Key == flag && x.ScopeTenantId == Guid.Empty, cancellationToken).ConfigureAwait(false)) return AppResponses.Failure<CatalogueEntryResponse>("Select a platform feature flag defined for this module.");
        var entry = existing ?? new ModuleCatalogueEntry { ModuleKey = moduleKey, Key = key, Kind = kind, Title = request.Title.Trim(), CreatedBy = command.ActorId };
        entry.Title = request.Title.Trim(); entry.Path = request.Path.Trim(); entry.ParentKey = string.IsNullOrWhiteSpace(request.ParentKey) ? null : request.ParentKey.Trim();
        entry.Icon = request.Icon; entry.Placement = request.Placement.Trim(); entry.DisplayOrder = request.DisplayOrder; entry.ShowInNavigation = request.ShowInNavigation;
        entry.PermissionKeys = permissions; entry.RoleNames = (request.RoleNames ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        entry.FeatureFlag = request.FeatureFlag; entry.IsActive = request.IsActive; if (existing is not null) entry.Version++;
        entry.UpdatedBy = command.ActorId; entry.UpdatedAt = DateTimeOffset.UtcNow;
        if (existing is null) await unit.Catalogue.CreateAsync(entry, cancellationToken).ConfigureAwait(false);
        if (existing is not null) await unit.Catalogue.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);
        await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success(entry.ToResponse());
    }
}
