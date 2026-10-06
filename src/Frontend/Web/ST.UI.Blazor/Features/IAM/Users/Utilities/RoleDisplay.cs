namespace ST.UI.Blazor.Features.IAM.Users.Utilities;

internal static class RoleDisplay
{
    private static readonly Dictionary<string, int> RolePriority = new(StringComparer.OrdinalIgnoreCase)
    {
        ["System Administrator"] = 0,
        ["Administrator"] = 10,
        ["IAM Administrator"] = 20,
        ["Security Administrator"] = 30,



    };

    public static string SelectPrimaryRole(IEnumerable<string>? roles, string fallback = "Signed in")
    {
        if (roles is null)
        {
            return fallback;
        }

        var candidates = roles
            .Where(static role => !string.IsNullOrWhiteSpace(role))
            .Select(static role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (candidates.Length == 0)
        {
            return fallback;
        }

        return candidates
            .OrderBy(static role => RolePriority.TryGetValue(role, out var priority) ? priority : 500)
            .ThenBy(static role => role, StringComparer.OrdinalIgnoreCase)
            .First();
    }
}
