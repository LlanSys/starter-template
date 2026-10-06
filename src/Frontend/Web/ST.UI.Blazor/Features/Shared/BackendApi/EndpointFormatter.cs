namespace ST.UI.Blazor.Features.Shared.BackendApi;

internal static class EndpointFormatter
{
    public static string Format(
        string endpoint,
        string version,
        IReadOnlyDictionary<string, string>? parameters = null,
        string? queryString = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        var formatted = endpoint.Replace("{version}", version, StringComparison.OrdinalIgnoreCase);
        if (parameters is not null)
        {
            foreach (var parameter in parameters)
            {
                formatted = formatted.Replace(
                    $"{{{parameter.Key}}}",
                    Uri.EscapeDataString(parameter.Value),
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        if (string.IsNullOrWhiteSpace(queryString))
        {
            return formatted;
        }

        return queryString[0] == '?'
            ? $"{formatted}{queryString}"
            : $"{formatted}?{queryString}";
    }
}
