using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ST.UI.Blazor.Features.IAM.Users.Utilities;

public static partial class OrganizationBranding
{
    public const string ProductName = "StarterTemplate";

    public static string DisplayName(string? organizationName, bool isPlatformWorkspace = false)
    {
        return isPlatformWorkspace || string.IsNullOrWhiteSpace(organizationName) ? ProductName : organizationName.Trim();
    }

    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "?";
        }
        var words = Words().Matches(name).Select(match => match.Value).ToArray();

        if (words.Length == 0)
        {
            return "?";
        }

        if (words.Length > 1)
        {
            return string.Concat(words.Take(3).Select(word => StringInfo.GetNextTextElement(word))).ToUpperInvariant();
        }

        var capitals = new List<string>();
        var elements = StringInfo.GetTextElementEnumerator(words[0]);

        while (elements.MoveNext() && capitals.Count < 3)
        {
            var element = elements.GetTextElement();

            if (Rune.IsUpper(element.EnumerateRunes().First()))
            {
                capitals.Add(element);
            }
        }
        return (capitals.Count > 1 ? string.Concat(capitals) : StringInfo.GetNextTextElement(words[0])).ToUpperInvariant();
    }

    [GeneratedRegex(@"[\p{L}\p{Nd}][\p{L}\p{M}\p{Nd}]*", RegexOptions.CultureInvariant, 100)]
    private static partial Regex Words();
}
