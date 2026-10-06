using System.Text.Json;
using ST.SharedKernel.Dtos.Common;
using MediatR;

namespace ST.Application.Features.IAM.Users.Commands;

public record RequestPasskeyRegistrationOptionsCommand : IRequest<AppResponse<JsonElement>>;
