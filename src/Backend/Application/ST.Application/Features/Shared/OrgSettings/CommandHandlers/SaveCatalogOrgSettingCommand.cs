using MediatR;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.SharedKernel.Dtos.Common;
namespace ST.Application.Features.Shared.OrgSettings.CommandHandlers;
public sealed record SaveCatalogOrgSettingCommand(string Key, string? Value, string? Revision, bool Reset, string ActorId) : IRequest<AppResponse<bool>>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> GroupVersionKeysToInvalidate => [CacheKeys.GroupVersion("tenant-settings")];
}
