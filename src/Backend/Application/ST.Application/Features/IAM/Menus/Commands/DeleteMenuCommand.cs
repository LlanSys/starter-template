using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Features.IAM.Contracts;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ST.Application.Features.IAM.Menus.Commands;


public sealed record DeleteMenuCommand(Guid Id, string UserId)
    : IRequest<AppResponse<bool>>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> DirectInvalidationKeys => [CacheKeys.Entity("menus", Id.ToString())];
    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [CacheKeys.GroupVersion("menus")];
}
