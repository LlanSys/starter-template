using ST.Domain.Features.Shared.OrgSettings.Entities;
using ST.Domain.Shared.Contracts.Repositories;

namespace ST.Domain.Features.Shared.OrgSettings.Contracts.Repositories;

public interface IOrgSettingRepository : IRepository<OrgSetting>
{
    Task LockSettingAsync(string key, CancellationToken cancellationToken);
    Task RecordChangeAsync(OrgSettingChange change, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrgSettingChange>> GetChangesAsync(CancellationToken cancellationToken);
}
