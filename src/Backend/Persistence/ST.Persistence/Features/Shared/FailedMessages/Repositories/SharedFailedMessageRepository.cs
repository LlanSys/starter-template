using ST.Domain.Features.Banking.Customers.Contracts.Repositories;
using ST.Domain.Features.HR.Employees.Contracts.Repositories;
using ST.Domain.Features.IAM.Users.Contracts.Repositories;
using ST.Domain.Shared.Contracts.Repositories;
using ST.Domain.Shared.Entities;
using ST.Persistence.Common.Repositories;
using ST.Persistence.Features.Shared.DataContext;

namespace ST.Persistence.Features.Shared.FailedMessages.Repositories;

internal sealed class SharedFailedMessageRepository(SharedDBContext context) : Repository<FailedMessage>(context), IFailedMessageRepository { }
