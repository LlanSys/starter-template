using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
namespace ST.Application.Features.IAM.Menus.Commands;
public sealed record ResetNavigationOverrideCommand(string PageKey) : IRequest<AppResponse<bool>>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [CacheKeys.GroupVersion("menus")];
}
