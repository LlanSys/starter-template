using ST.Domain.Features.Shared.FailedMessages.Entities;
using ST.Domain.Shared.Contracts.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Features.Shared.FailedMessages.Contracts.Repositories;

public interface IFailedMessageRepository : IRepository<FailedMessage>
{
}
