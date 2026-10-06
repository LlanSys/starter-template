using ST.Domain.Features.IAM.Users.Contracts.Repositories;
using ST.Domain.Shared.Contracts.Repositories;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Persistence.Common.Repositories;
using ST.Persistence.Features.IAM.DataContext;

namespace ST.Persistence.Features.IAM.Users.Repositories;

internal sealed class IamUserRepository(IamDBContext context) : Repository<AppUser>(context), IUserRepository { }
