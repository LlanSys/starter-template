using ST.Domain.Features.IAM.Menus.Entities;
namespace ST.Application.Features.IAM.Menus;
public static class NavigationHierarchy
{
    public static IReadOnlyList<MenuItem> Visible(IEnumerable<MenuItem> items, Func<MenuItem, bool> permitted)
    {
        var lookup = items.ToDictionary(x => x.Id);
        var resolved = new Dictionary<Guid, bool>();
        bool Visit(MenuItem item, HashSet<Guid> path)
        {
            if (resolved.TryGetValue(item.Id, out var ready)) return ready;
            if (!permitted(item) || path.Count >= 64 || !path.Add(item.Id)) return false;
            ready = item.ParentId is not { } parent || (lookup.TryGetValue(parent, out var ancestor) && Visit(ancestor, path));
            path.Remove(item.Id); resolved[item.Id] = ready; return ready;
        }
        return lookup.Values.Where(item => Visit(item, [])).ToArray();
    }
}
