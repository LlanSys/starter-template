using System.Text.Json;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;

namespace ST.Application.Features.IAM.Users.Commands;

public record RequestPasskeyLoginOptionsCommand(string? Username) : IRequest<AppResponse<PasskeyLoginOptionsResponse>>;
