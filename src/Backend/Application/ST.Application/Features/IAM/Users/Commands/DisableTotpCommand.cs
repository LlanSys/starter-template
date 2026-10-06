using ST.SharedKernel.Dtos.Common;
using MediatR;

namespace ST.Application.Features.IAM.Users.Commands;

public sealed record DisableTotpCommand(string UserId, string DisabledBy)
    : IRequest<AppResponse<bool>>;
