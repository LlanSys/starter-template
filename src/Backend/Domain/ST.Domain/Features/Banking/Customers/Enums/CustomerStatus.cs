using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ST.Domain.Features.Banking.Customers.Enums;

public enum CustomerStatus
{
    [Description("Draft")]
    Draft = 1,

    [Description("Active")]
    Active = 2,

    [Description("Suspended")]
    Suspended = 3,

    [Description("Closed")]
    Closed = 4,

    [Description("Pending Approval")]
    PendingApproval = 5
}
