using MediatR;
using ST.SharedKernel.Dtos.Common;
namespace ST.Application.Features.IAM.Menus.Commands;
public sealed record ResetNavigationOverrideCommand(string PageKey) : IRequest<AppResponse<bool>>, ST.Application.Contracts.Interfaces.Common.ICacheInvalidatorRequest
{
    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [ST.Application.Utilities.CacheKeys.GroupVersion("menus")];
}
