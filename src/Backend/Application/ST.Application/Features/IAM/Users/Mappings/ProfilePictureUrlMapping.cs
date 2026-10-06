namespace ST.Application.Features.IAM.Users.Mappings;

public static class ProfilePictureUrlMapping
{
    private static readonly Uri CurrentUserProfilePictureRoute =
        new("/api/v1/iam/users/me/profile-picture/content", UriKind.Relative);

    public static Uri? ToCurrentUserRoute(Uri? storedProfilePictureUrl)
        => storedProfilePictureUrl is null ? null : CurrentUserProfilePictureRoute;
}
