using ST.Application.Contracts.Interfaces.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using MediatR;
using System.Collections.Generic;

namespace ST.Application.Features.IAM.Users.Queries;

public record GetPasskeysQuery : IRequest<AppResponse<IReadOnlyList<PasskeyResponse>>>, ICachableRequest
{
    public string CacheGroup => "passkeys";
    public string Discriminator => string.Empty;
    public string? CacheUserId => null;
    public bool IsVersioned => false;
    public bool BypassCache => true;
}
