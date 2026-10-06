using Asp.Versioning;
using ST.Api.Common.Controllers;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.Queries;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ST.Api.Features.IAM.Users.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/iam/users/me")]
[ApiController]
[Authorize]
public sealed class ProfileController(ISender sender) : BaseController
{
    [HttpPost("profile-picture")]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> UpdateProfilePicture(IFormFile file)
    {
        if (file is null)
        {
            return BadRequest(AppResponses.Failure<ProfilePictureResponse>("Profile picture is required."));
        }

        var userId = GetUserId();
        using var stream = file.OpenReadStream();
        var response = await sender
            .Send(new UpdateProfilePictureCommand(
                userId,
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                userId))
            .ConfigureAwait(false);

        return HandleResponse(response);
    }

    [HttpGet("profile-picture/content")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GetProfilePicture()
    {
        var response = await sender
            .Send(new GetCurrentUserProfilePictureQuery(GetUserId()))
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            return HandleResponse(response);
        }

        return File(response.Data.Content, response.Data.ContentType, enableRangeProcessing: true);
    }

    private string GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Authenticated user id was not found.");
        }

        return userId;
    }
}
