namespace ST.Application.Features.ControlPlane.Tenants.Commands;

internal static class WorkspaceClientOrigin
{
    public static bool TryNormalize(string? candidate, out string? origin)
    {
        origin = null;
        if (candidate is null)
        {
            return true;
        }

        if (candidate.Length > 2048 || !Uri.TryCreate(candidate, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && !(uri.IsLoopback && uri.Scheme == Uri.UriSchemeHttp))
            || uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        origin = uri.GetLeftPart(UriPartial.Authority);
        return true;
    }
}
