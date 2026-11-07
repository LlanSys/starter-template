using MediatR;
using ST.Application.Features.ControlPlane.Modules;
using ST.Application.Features.IAM.Menus.Mappings;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Menus.Dtos;
namespace ST.Application.Features.IAM.Menus.Queries;
internal sealed class GetNavigationMenusQueryHandler(IEffectiveNavigation navigation, IEffectiveModulePolicy policy) : IRequestHandler<GetNavigationMenusQuery, AppResponse<IReadOnlyList<MenuResponse>>>
{
    public async Task<AppResponse<IReadOnlyList<MenuResponse>>> Handle(GetNavigationMenusQuery query, CancellationToken cancellationToken)
    {
        var access = await policy.GetAsync(cancellationToken).ConfigureAwait(false);
        var rows = await navigation.GetAsync(cancellationToken).ConfigureAwait(false);
        var permissions = query.PermissionKeys.Where(key => key.StartsWith("control_plane.", StringComparison.OrdinalIgnoreCase) || access.PermissionAvailable(key)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var roles = (query.RoleNames ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var visible = rows.ToDictionary(x => x.Menu.Id, x => x.Available && x.Menu.IsActive && x.Menu.IsVisibleTo(roles, permissions, query.HasFullAccess)
            && x.Menu.RequiredPermissionKeys.All(key => key.StartsWith("control_plane.", StringComparison.OrdinalIgnoreCase) ? permissions.Contains(key) : access.PermissionAvailable(key))
            && (x.Definition is null || x.Definition.RoleNames.Length == 0 || x.Definition.RoleNames.Any(roles.Contains)));
        var tree = NavigationHierarchy.Visible(rows.Select(x => x.Menu), menu => visible[menu.Id])
            .Where(menu => string.Equals(menu.Placement, query.Placement, StringComparison.OrdinalIgnoreCase)).ToList().ToTree();
        return AppResponses.Success<IReadOnlyList<MenuResponse>>(tree);
    }
}
