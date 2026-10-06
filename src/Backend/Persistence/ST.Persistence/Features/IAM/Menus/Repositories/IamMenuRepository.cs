using ST.Domain.Features.IAM.Menus.Contracts.Repositories;
using ST.Domain.Features.IAM.Menus.Entities;
using ST.Persistence.Common.Repositories;
using ST.Persistence.Features.IAM.DataContext;

namespace ST.Persistence.Features.IAM.Menus.Repositories;

public sealed class IamMenuRepository(IamDBContext context)
    : Repository<MenuItem>(context), IMenuRepository;
