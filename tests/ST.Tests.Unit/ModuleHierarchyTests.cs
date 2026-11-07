using ST.Application.Features.ControlPlane.Modules;
using ST.Application.Features.IAM.Menus;
using ST.Domain.Features.IAM.Menus.Entities;
namespace ST.Tests.Unit;
public sealed class ModuleHierarchyTests
{
    [Fact]
    public void UnavailableParentSuppressesTheWholeSubtreeAcrossPlacements()
    {
        var parent = Menu(null, "parent", "Sidebar"); var child = Menu(parent.Id, "child", "AdminCenter"); var grandchild = Menu(child.Id, "grandchild", "AdminCenter");
        Assert.Empty(NavigationHierarchy.Visible([parent, child, grandchild], x => x.Id != parent.Id));
        Assert.Equal(3, NavigationHierarchy.Visible([parent, child, grandchild], _ => true).Count);
    }
    [Fact]
    public void MissingAncestorsAndCyclesDoNotBecomeRootEntries()
    {
        var first = Menu(Guid.CreateVersion7(), "first", "Sidebar");
        Assert.Empty(NavigationHierarchy.Visible([first], _ => true));
        var second = Menu(first.Id, "second", "Sidebar");
        first.Update(second.Id, first.Key, first.Title, "", first.Url, "Menu", first.Placement, null, "core", 0, true, "test");
        Assert.Empty(NavigationHierarchy.Visible([first, second], _ => true));
    }
    [Fact]
    public void DependenciesFailClosedForMissingEntitlementsAndCycles()
    {
        var graph = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase) { ["core"] = [], ["iam"] = ["core"], ["loans"] = ["iam"], ["members"] = ["core"] };
        var available = ModuleDependencyGraph.Resolve(graph, new HashSet<string>(["core", "loans", "members"], StringComparer.OrdinalIgnoreCase));
        Assert.Contains("members", available); Assert.DoesNotContain("loans", available);
        graph["core"] = ["loans"];
        Assert.False(ModuleDependencyGraph.IsAcyclic(graph));
        Assert.Empty(ModuleDependencyGraph.Resolve(graph, graph.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase)));
    }
    private static MenuItem Menu(Guid? parent, string key, string placement) => MenuItem.Create(parent, key, key, "", "/" + key, "Menu", placement, null, "core", 0, "test");
}
