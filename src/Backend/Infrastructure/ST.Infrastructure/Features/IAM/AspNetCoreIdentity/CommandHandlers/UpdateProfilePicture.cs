using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;

internal sealed class UpdateProfilePicture(
    UserManager<AppUser> userManager,
    IProfilePictureStorage storage,
    IOptions<ProfileImageStorageSettings> storageOptions,
    ILogger<UpdateProfilePicture> logger)
    : IRequestHandler<UpdateProfilePictureCommand, AppResponse<ProfilePictureResponse>>
{
    private readonly ProfileImageStorageSettings _settings = storageOptions.Value;

    public async Task<AppResponse<ProfilePictureResponse>> Handle(UpdateProfilePictureCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            var validationError = Validate(command);
            if (validationError is not null)
            {
                return AppResponses.Failure<ProfilePictureResponse>(validationError);
            }

            var user = await userManager.FindByIdAsync(command.UserId).ConfigureAwait(false);
            if (user is null)
            {
                return AppResponses.Failure<ProfilePictureResponse>("User account was not found.");
            }

            var profilePictureUrl = await storage
                .SaveAsync(command.UserId, command.Content, command.FileName, command.ContentType, cancellationToken)
                .ConfigureAwait(false);

            user.SetProfilePicture(profilePictureUrl, command.UpdatedBy);

            var result = await userManager.UpdateAsync(user).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(static error => error.Description));
                return AppResponses.Failure<ProfilePictureResponse>($"Profile picture could not be updated: {errors}");
            }

            return AppResponses.Success(
                "Profile picture updated.",
                new ProfilePictureResponse(ProfilePictureUrlMapping.ToCurrentUserRoute(profilePictureUrl)!));
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogProfilePictureUpdateError(logger, command.UserId, ex);
            throw;
        }
    }

    private string? Validate(UpdateProfilePictureCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return "Authenticated user was not resolved.";
        }

        if (command.Length <= 0)
        {
            return "Profile picture is required.";
        }

        if (command.Length > _settings.MaxBytes)
        {
            return $"Profile picture must be {FormatBytes(_settings.MaxBytes)} or smaller.";
        }

        if (!_settings.AllowedContentTypes.Contains(command.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return "Profile picture must be a JPEG, PNG, or WebP image.";
        }

        return null;
    }

    private static string FormatBytes(long bytes)
        => bytes >= 1_048_576
            ? $"{bytes / 1_048_576m:0.#} MB"
            : $"{bytes / 1024m:0.#} KB";
}
