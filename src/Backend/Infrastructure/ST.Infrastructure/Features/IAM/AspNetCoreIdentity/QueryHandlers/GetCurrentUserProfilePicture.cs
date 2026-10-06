using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.IAM.Users.Queries;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.QueryHandlers;

internal sealed class GetCurrentUserProfilePicture(
    UserManager<AppUser> userManager,
    IProfilePictureStorage storage,
    ILogger<GetCurrentUserProfilePicture> logger)
    : IRequestHandler<GetCurrentUserProfilePictureQuery, AppResponse<ProfilePictureFile>>
{
    public async Task<AppResponse<ProfilePictureFile>> Handle(
        GetCurrentUserProfilePictureQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return AppResponses.Failure<ProfilePictureFile>("Authenticated user was not resolved.");
        }

        var user = await userManager.FindByIdAsync(query.UserId).ConfigureAwait(false);
        if (user is null || user.ProfilePictureUrl is null)
        {
            return AppResponses.Failure<ProfilePictureFile>("Profile picture was not found.");
        }

        ProfilePictureFile? profilePicture;
        try
        {
            profilePicture = await storage
                .OpenReadAsync(user.ProfilePictureUrl, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            ServiceLogDefinitions.LogProfilePictureReadUnavailable(logger, user.Id, ex);
            return AppResponses.Failure<ProfilePictureFile>(
                AppError.DependencyUnavailable("Profile picture storage is temporarily unavailable."));
        }

        return profilePicture is null
            ? AppResponses.Failure<ProfilePictureFile>("Profile picture was not found.")
            : AppResponses.Success("Profile picture loaded.", profilePicture);
    }
}
