using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ST.Domain.Features.Banking.Customers.Enums;

public enum DirectorRelationType
{
    [Description("Director")]
    Director = 1,

    [Description("Shareholder")]
    Shareholder = 2,

    [Description("Signatory")]
    Signatory = 3,

    [Description("Beneficial Owner")]
    BeneficialOwner = 4,

    [Description("Guarantor")]
    Guarantor = 5
}
