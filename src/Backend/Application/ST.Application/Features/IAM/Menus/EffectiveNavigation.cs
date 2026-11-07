using ST.Application.Features.ControlPlane.Modules;
using ST.Domain.Features.ControlPlane.Modules.Entities;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Menus.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Menus.Dtos;
using ST.Domain.Shared.Contracts.Common;
namespace ST.Application.Features.IAM.Menus;
public sealed class EffectiveNavigation(IIamUnitOfWork unit, IEffectiveModulePolicy policy, ICurrentActorProvider actor, TimeProvider timeProvider) : IEffectiveNavigation
{
    public async Task<IReadOnlyList<ResolvedNavigationItem>> GetAsync(CancellationToken cancellationToken = default)
    {
        var access = await policy.GetAsync(cancellationToken).ConfigureAwait(false);
        var pages = access.Catalogue.Where(x => x.Kind == ModuleCatalogueKind.Page).ToArray();
        var legacy = await unit.MenuRepository.ListAsync(ct: cancellationToken).ConfigureAwait(false);
        var overrides = await unit.NavigationOverrides.ListAsync(ct: cancellationToken).ConfigureAwait(false);
        var matches = pages.ToDictionary(page => page.Key, page => legacy.FirstOrDefault(menu => menu.Key == page.Key || (menu.Url == page.Path && menu.Placement == page.Placement)), StringComparer.OrdinalIgnoreCase);
        var ids = pages.ToDictionary(page => page.Key, page => matches[page.Key]?.Id ?? page.Id, StringComparer.OrdinalIgnoreCase);
        var consumed = matches.Values.OfType<MenuItem>().Select(x => x.Id).ToHashSet();
        var rows = new List<ResolvedNavigationItem>();
        foreach (var page in pages)
        {
            var old = matches[page.Key]; var change = overrides.FirstOrDefault(x => x.PageKey == page.Key);
            var overrideVersion = change?.Version ?? 0;
            if (change?.UsePlatformDefaults == true) { old = null; change = null; }
            var parentId = change is not null ? change.ParentId : old is not null ? old.ParentId : (page.ParentKey is { } parent ? ids.GetValueOrDefault(parent) : (Guid?)null);
            if (parentId == Guid.Empty) parentId = null;
            var menu = MenuItem.Create(parentId, page.Key, change?.Title ?? (page.Key == "admin-menus" && old?.Title == "Menus" ? page.Title : old?.Title) ?? page.Title, old?.Description ?? "", page.Path,
                change?.Icon ?? old?.Icon ?? page.Icon, change?.Placement ?? old?.Placement ?? page.Placement,
                null, page.ModuleKey, change?.DisplayOrder ?? old?.DisplayOrder ?? page.DisplayOrder, "System");
            menu.Id = ids[page.Key];
            menu.Update(timeProvider.GetUtcNow(), menu.ParentId, menu.Key, menu.Title, menu.Description, menu.Url, menu.Icon, menu.Placement, null, page.ModuleKey, menu.DisplayOrder, change?.IsActive ?? old?.IsActive ?? (page.IsActive && page.ShowInNavigation), change?.UpdatedBy ?? "System");
            menu.SetVisibility(change?.RoleNames ?? old?.RequiredRoleNames ?? page.RoleNames,
                page.PermissionKeys.Concat(change?.PermissionKeys ?? old?.RequiredPermissionKeys ?? []).Append(old?.RequiredPermissionKey ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray());
            rows.Add(new(menu, page, access.EntryAvailable(page), change is not null, overrideVersion));
        }
        foreach (var menu in legacy.Where(x => !consumed.Contains(x.Id)))
        {
            var owner = ModulePageBindings.Owner(menu.Url) ?? menu.RequiredModule;
            rows.Add(new(menu, null, owner is null || access.Modules.Contains(owner), false, 0));
        }
        return rows;
    }
    public async Task<AppResponse<MenuResponse>> SaveOverrideAsync(string pageKey, UpdateMenuRequest request, string actor, CancellationToken cancellationToken = default)
    {
        var rows = await GetAsync(cancellationToken).ConfigureAwait(false);
        var selected = rows.FirstOrDefault(x => x.Definition?.Key == pageKey && x.Available);
        if (selected?.Definition is not { } definition) return AppResponses.Failure<MenuResponse>("This page is not available for your tenant.");
        if (!string.Equals(request.Url, definition.Path, StringComparison.OrdinalIgnoreCase)) return AppResponses.Failure<MenuResponse>("An existing navigation override cannot change its registered page.");
        if (!string.Equals(request.Placement, definition.Placement, StringComparison.OrdinalIgnoreCase) && request.Placement is not ("Sidebar" or "AdminCenter" or "Features")) return AppResponses.Failure<MenuResponse>("Select a valid navigation location.");
        if (request.ParentId.HasValue)
        {
            var parent = rows.FirstOrDefault(x => x.Menu.Id == request.ParentId && x.Available);
            if (parent is null || !parent.Menu.IsActive || parent.Menu.Placement != request.Placement) return AppResponses.Failure<MenuResponse>("Select an available Parent in the same navigation location.");
            if (parent.Definition is { } parentDefinition && !string.Equals(parentDefinition.ModuleKey, definition.ModuleKey, StringComparison.OrdinalIgnoreCase) && parentDefinition.ModuleKey != "core") return AppResponses.Failure<MenuResponse>("The Parent must belong to this module or Core.");
            var seen = new HashSet<Guid> { selected.Menu.Id };
            for (var cursor = parent.Menu; cursor is not null; cursor = rows.FirstOrDefault(x => x.Menu.Id == cursor.ParentId)?.Menu)
                if (!seen.Add(cursor.Id) || seen.Count > 64) return AppResponses.Failure<MenuResponse>("The Parent would create a cycle or exceed 64 levels.");
        }
        var access = await policy.GetAsync(cancellationToken).ConfigureAwait(false);
        var validation = await MenuVisibilityValidation.ValidateAsync(unit, request.RequiredRoleNames, request.RequiredPermissionKeys, cancellationToken, access).ConfigureAwait(false);
        if (validation is not null) return AppResponses.Failure<MenuResponse>(validation);
        if ((request.RequiredPermissionKeys ?? []).Any(key => !access.PermissionAvailable(key))) return AppResponses.Failure<MenuResponse>("Select permissions available to your tenant.");
        var row = await unit.NavigationOverrides.FirstOrDefaultAsync(x => x.PageKey == pageKey, cancellationToken).ConfigureAwait(false);
        if (request.ExpectedVersion is { } version && version != (row?.Version ?? 0)) return AppResponses.Failure<MenuResponse>("This override changed. Reload it before saving.");
        if (row is null) { row = new NavigationOverride { PageKey = pageKey, Title = request.Title.Trim(), CreatedBy = actor }; await unit.NavigationOverrides.CreateAsync(row, cancellationToken).ConfigureAwait(false); }
        else row.Version++;
        row.UsePlatformDefaults = false;
        row.Title = request.Title.Trim(); row.ParentId = request.ParentId; row.Icon = request.Icon; row.Placement = request.Placement; row.DisplayOrder = request.DisplayOrder; row.IsActive = request.IsActive;
        row.RoleNames = (request.RequiredRoleNames ?? []).ToArray(); row.PermissionKeys = (request.RequiredPermissionKeys ?? []).ToArray(); row.UpdatedBy = actor; row.UpdatedAt = timeProvider.GetUtcNow();
        await unit.NavigationOverrides.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
        await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
        var updated = (await GetAsync(cancellationToken).ConfigureAwait(false)).Single(x => x.Definition?.Key == pageKey);
        return AppResponses.Success(updated.ToResponse());
    }
    public async Task<AppResponse<bool>> ResetOverrideAsync(string pageKey, CancellationToken cancellationToken = default)
    {
        var selected = (await GetAsync(cancellationToken).ConfigureAwait(false)).FirstOrDefault(x => x.Definition?.Key == pageKey && x.Available);
        if (selected?.Definition is not { } page) return AppResponses.Failure<bool>("Navigation default not found for this tenant.");
        var row = await unit.NavigationOverrides.FirstOrDefaultAsync(x => x.PageKey == pageKey, cancellationToken).ConfigureAwait(false);
        if (row is null) { row = new NavigationOverride { PageKey = pageKey, Title = page.Title, CreatedBy = actor.ActorId }; await unit.NavigationOverrides.CreateAsync(row, cancellationToken).ConfigureAwait(false); }
        row.UsePlatformDefaults = true; row.Version++; row.UpdatedAt = timeProvider.GetUtcNow(); row.UpdatedBy = actor.ActorId;
        await unit.NavigationOverrides.UpdateAsync(row, cancellationToken).ConfigureAwait(false);
        await unit.CompleteAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success(true);
    }
}
