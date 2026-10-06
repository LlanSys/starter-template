using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;

namespace ST.Application.Features.IAM.Users.Commands;

public sealed record UpdateProfilePictureCommand(
    string UserId,
    Stream Content,
    string FileName,
    string ContentType,
    long Length,
    string UpdatedBy)
    : IRequest<AppResponse<ProfilePictureResponse>>;
