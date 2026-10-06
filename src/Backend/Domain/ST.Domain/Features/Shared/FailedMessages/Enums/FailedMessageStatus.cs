using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Features.Shared.FailedMessages.Enums;

public enum FailedMessageStatus
{
    Transient = 1,
    Permanent = 2,
    ManualRetry = 3
}
