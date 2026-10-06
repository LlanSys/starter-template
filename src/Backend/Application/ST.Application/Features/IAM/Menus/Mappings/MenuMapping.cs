using ST.Domain.Features.IAM.Menus.Entities;
using ST.SharedKernel.Features.IAM.Menus.Dtos;

namespace ST.Application.Features.IAM.Menus.Mappings;

public static class MenuMapping
{
    public static MenuResponse ToMenuResponse(this MenuItem menu, IReadOnlyList<MenuResponse>? children = null)
    {
        ArgumentNullException.ThrowIfNull(menu);

        return new MenuResponse(
            menu.Id,
            menu.ParentId,
            menu.DepartmentId,
            menu.Key,
            menu.Title,
            menu.Description,
            menu.Url,
            menu.Icon,
            menu.Placement,
            menu.RequiredPermissionKey,
            menu.RequiredModule,
            menu.DisplayOrder,
            menu.IsActive,
            children ?? []);
    }

    public static IReadOnlyList<MenuResponse> ToTree(this IReadOnlyList<MenuItem> menus)
    {
        var ids = menus.Select(static menu => menu.Id).ToHashSet();
        var rootParentIds = menus
            .Where(menu => menu.ParentId is null || !ids.Contains(menu.ParentId.Value))
            .Select(static menu => menu.ParentId)
            .Distinct()
            .ToList();

        var lookup = menus.ToLookup(static menu => menu.ParentId);

        return [.. rootParentIds.SelectMany(Build).OrderBy(static menu => menu.DisplayOrder).ThenBy(static menu => menu.Title)];

        List<MenuResponse> Build(Guid? parentId)
            => [.. lookup[parentId]
                .OrderBy(static menu => menu.DisplayOrder).ThenBy(static menu => menu.Title)
                .Select(child => child.ToMenuResponse(Build(child.Id)))];
    }
}
