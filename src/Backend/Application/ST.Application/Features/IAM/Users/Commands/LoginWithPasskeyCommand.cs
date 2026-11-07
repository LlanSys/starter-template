using System.Text.Json;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;

namespace ST.Application.Features.IAM.Users.Commands;

public sealed record LoginWithPasskeyCommand(string? Username, Guid CorrelationId, JsonElement AssertionResponse) : IRequest<AppResponse<LoginResponse>>;
