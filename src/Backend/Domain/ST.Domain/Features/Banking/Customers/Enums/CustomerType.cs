using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ST.Domain.Features.Banking.Customers.Enums;

public enum CustomerType
{
    [Description("Individual")]
    Individual = 1,

    [Description("Corporate")]
    Corporate = 2,

    [Description("Institutional")]
    Institutional = 3,

    [Description("SME")]
    SmallMediumEnterprise = 4,

    [Description("Enterprise")]
    Enterprise = 5
}
