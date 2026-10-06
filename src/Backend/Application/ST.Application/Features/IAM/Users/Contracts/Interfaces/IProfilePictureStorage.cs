namespace ST.Application.Features.IAM.Users.Contracts.Interfaces;

public interface IProfilePictureStorage
{
    Task<Uri> SaveAsync(
        string userId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<ProfilePictureFile?> OpenReadAsync(
        Uri profilePictureUri,
        CancellationToken cancellationToken = default);
}
