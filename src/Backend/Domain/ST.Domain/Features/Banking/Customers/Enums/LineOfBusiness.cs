using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ST.Domain.Features.Banking.Customers.Enums;

public enum LineOfBusiness
{
    [Description("Agriculture")]
    Agriculture = 1,

    [Description("Manufacturing")]
    Manufacturing = 2,

    [Description("Technology")]
    Technology = 3,

    [Description("Financial Services")]
    FinancialServices = 4,

    [Description("Retail")]
    Retail = 5,

    [Description("Services")]
    Services = 6,

    [Description("Proprietary")]
    Proprietary = 7,

    [Description("Trading")]
    Trading = 8

}
