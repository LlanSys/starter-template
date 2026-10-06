using ST.SharedKernel.Dtos.Common;
using MediatR;

namespace ST.Application.Features.IAM.Users.Commands;

public sealed record InviteAdminUserCommand(string UserId) : IRequest<AppResponse<bool>>;
