using MediatR;
using ST.Application.Contracts.Interfaces.Common;
using ST.Domain.Features.Shared.Contracts;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.OrgSettings.Dtos;
namespace ST.Application.Features.Shared.OrgSettings.QueryHandlers;
public sealed record GetOrgSettingChangesQuery : IRequest<AppResponse<IReadOnlyList<OrgSettingChangeResponse>>>, ICachableRequest
{
    public string CacheGroup => "tenant-settings";
    public string Discriminator => "catalogue-history-v1";
    public string? CacheUserId => null;
    public bool IsVersioned => true;
}
internal sealed class GetOrgSettingChangesHandler(ISharedUnitOfWork unit) : IRequestHandler<GetOrgSettingChangesQuery, AppResponse<IReadOnlyList<OrgSettingChangeResponse>>>
{
    public async Task<AppResponse<IReadOnlyList<OrgSettingChangeResponse>>> Handle(GetOrgSettingChangesQuery request, CancellationToken cancellationToken)
    {
        var rows = await unit.OrgSettingRepository.GetChangesAsync(cancellationToken).ConfigureAwait(false);
        return AppResponses.Success<IReadOnlyList<OrgSettingChangeResponse>>(rows.Select(row =>
            new OrgSettingChangeResponse(row.Key, row.PreviousValue, row.Value, row.Reset, row.CreatedBy, row.CreatedAt)).ToArray());
    }
}
