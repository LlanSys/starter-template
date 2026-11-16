using ST.Domain.Features.Shared.OrgSettings.Contracts.Repositories;
using ST.Domain.Features.Shared.OrgSettings.Entities;
using ST.Persistence.Common.Repositories;
using ST.Persistence.Features.Shared.DataContext;
using Microsoft.EntityFrameworkCore;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace ST.Persistence.Features.Shared.OrgSettings.Repositories;

public class OrgSettingRepository(SharedDBContext dbContext)
    : Repository<OrgSetting>(dbContext), IOrgSettingRepository
{
    public async Task LockSettingAsync(string key, CancellationToken cancellationToken)
    {
        var resource = $"ST:OrgSetting:{dbContext.CurrentTenantId:D}:{key}";

        if (dbContext.Database.IsSqlServer())
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; IF @r < 0 THROW 51000, 'Setting lock unavailable', 1;", cancellationToken).ConfigureAwait(false);
        }
        else if (dbContext.Database.IsNpgsql())
        {
            var lockKey = BinaryPrimitives.ReadInt64LittleEndian(SHA256.HashData(Encoding.UTF8.GetBytes(resource)));
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockKey})", cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task RecordChangeAsync(OrgSettingChange change, CancellationToken cancellationToken)
    {
        await dbContext.OrgSettingChanges.AddAsync(change, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OrgSettingChange>> GetChangesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.OrgSettingChanges.AsNoTracking().OrderByDescending(change => change.CreatedAt)
            .ThenByDescending(change => change.Id).Take(100).ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
