using ST.Domain.Features.IAM.Permissions.Contracts.Repositories;
using ST.Domain.Features.IAM.Permissions.Entities;
using ST.Persistence.Common.Repositories;
using ST.Persistence.Features.IAM.DataContext;

namespace ST.Persistence.Features.IAM.Permissions.Repositories;

public sealed class IamPermissionRepository(IamDBContext context)
    : Repository<Permission>(context), IPermissionRepository;
