using ST.SharedKernel.Dtos.Common;
using MediatR;

namespace ST.Application.Features.IAM.Users.Commands;

public sealed record ProcessSsoLoginCommand(
    string Email,
    string FirstName,
    string LastName,
    string Provider,
    string ProviderKey) : IRequest<AppResponse<string>>;
