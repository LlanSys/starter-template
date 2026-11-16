namespace ST.SharedKernel.Features.ControlPlane.Tenants;

public static class FrontendOrigin
{
    public const string HeaderName = "X-Frontend-Origin";

    public static bool TryNormalize(string? candidate, out string? origin)
    {
        origin = null;

        if (string.IsNullOrWhiteSpace(candidate) || candidate.Length > 2048 || candidate.Any(char.IsControl)
            || candidate.Contains('\\', StringComparison.Ordinal)
            || !Uri.TryCreate(candidate.Trim(), UriKind.Absolute, out var uri)
            || uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }
        var host = uri.IdnHost.TrimEnd('.').ToLowerInvariant();
        var local = uri.IsLoopback || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase);

        if (host.Length > 253 || (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && local)))
        {
            return false;
        }
        origin = new UriBuilder(uri.Scheme, host, uri.IsDefaultPort ? -1 : uri.Port).Uri.GetLeftPart(UriPartial.Authority);
        return true;
    }
}
