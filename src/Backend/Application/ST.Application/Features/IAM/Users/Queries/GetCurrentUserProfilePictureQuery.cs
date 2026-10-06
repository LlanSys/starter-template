using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.SharedKernel.Dtos.Common;
using MediatR;

namespace ST.Application.Features.IAM.Users.Queries;

public sealed record GetCurrentUserProfilePictureQuery(string UserId)
    : IRequest<AppResponse<ProfilePictureFile>>, ICachableRequest
{
    public string CacheGroup => "profile-pictures";

    public string Discriminator => UserId;

    public string? CacheUserId => UserId;

    public bool IsVersioned => false;

    public bool BypassCache => true;
}
