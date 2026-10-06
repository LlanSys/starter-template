using ST.SharedKernel.Dtos.Common;
using MediatR;
using System.Text.Json;

namespace ST.Application.Features.Shared.Payments.CommandHandlers.Mpesa;

public sealed record ProcessMpesaC2BValidationCommand(JsonElement Payload) : IRequest<AppResponse<string>>;
