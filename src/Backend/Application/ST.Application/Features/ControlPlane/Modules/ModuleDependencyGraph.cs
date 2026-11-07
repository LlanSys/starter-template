namespace ST.Application.Features.ControlPlane.Modules;
public static class ModuleDependencyGraph
{
    public static IReadOnlySet<string> Resolve(IReadOnlyDictionary<string, string[]> dependencies, IReadOnlySet<string> candidates)
    {
        var resolved = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        bool Visit(string key, HashSet<string> path)
        {
            if (resolved.TryGetValue(key, out var ready)) return ready;
            if (!candidates.Contains(key) || !dependencies.ContainsKey(key) || path.Count >= 64 || !path.Add(key)) return false;
            ready = dependencies[key].All(parent => Visit(parent, path));
            path.Remove(key); resolved[key] = ready; return ready;
        }
        return candidates.Where(key => Visit(key, new(StringComparer.OrdinalIgnoreCase))).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
    public static bool IsAcyclic(IReadOnlyDictionary<string, string[]> dependencies) => Resolve(dependencies, dependencies.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase)).Count == dependencies.Count;
}
