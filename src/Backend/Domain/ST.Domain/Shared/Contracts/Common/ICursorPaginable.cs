using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Shared.Contracts.Common;

public interface ICursorPaginable
{
    Guid Id { get; }
    DateTimeOffset CreatedAt { get; }
}
