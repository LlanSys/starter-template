using ST.Domain.Features.Shared.OrgSettings.Contracts.Repositories;
using ST.Domain.Features.Shared.OrgSettings.Entities;
using ST.Persistence.Common.Repositories;
using ST.Persistence.Features.Shared.DataContext;
using Microsoft.EntityFrameworkCore;

namespace ST.Persistence.Features.Shared.OrgSettings.Repositories;

public class OrgSettingRepository(SharedDBContext dbContext)
    : Repository<OrgSetting>(dbContext), IOrgSettingRepository
{
    private readonly SharedDBContext _dbContext = dbContext;
}
