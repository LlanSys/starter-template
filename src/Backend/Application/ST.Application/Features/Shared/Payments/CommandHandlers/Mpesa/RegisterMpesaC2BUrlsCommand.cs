using ST.SharedKernel.Dtos.Common;
using MediatR;

namespace ST.Application.Features.Shared.Payments.CommandHandlers.Mpesa;

public sealed record RegisterMpesaC2BUrlsCommand() : IRequest<AppResponse<string>>;
