using MediatR;
using ST.SharedKernel.Dtos.Common;

namespace ST.Application.Features.IAM.Users.Commands;

public sealed record ProcessSsoLoginCommand(
    string Provider, string ProviderKey, Guid? InvitationId = null, string? InvitationToken = null, string? VerifiedEmail = null)
    : IRequest<AppResponse<string>>;
