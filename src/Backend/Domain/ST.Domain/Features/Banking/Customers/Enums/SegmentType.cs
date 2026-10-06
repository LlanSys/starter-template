using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ST.Domain.Features.Banking.Customers.Enums;

public enum SegmentType
{
    [Description("Public Limited Company")]
    PublicLimitedCompany = 1,

    [Description("Private Limited Company")]
    PrivateLimitedCompany = 2,

    [Description("Sole Proprietorship")]
    SoleProprietorship = 3,

    [Description("Corporate")]
    Corporate = 4,

    [Description("Retail")]
    Retail = 5,

    [Description("Small Medium Enterprise")]
    SME = 6,

    [Description("Small Medium Enterprise")]
    Individual = 7





}
