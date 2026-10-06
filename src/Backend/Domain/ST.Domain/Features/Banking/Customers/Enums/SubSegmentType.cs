using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ST.Domain.Features.Banking.Customers.Enums;

public enum SubSegmentType
{
    [Description("Local")]
    Local = 1,

    [Description("Multinational")]
    Multinational = 2,

    [Description("Government Owned")]
    GovernmentOwned = 3,

    [Description("Partnership")]
    Partnership = 4,

    [Description("PrivateLimitedCompany")]
    PrivateLimitedCompany = 5,

    [Description("PublicLimitedCompany")]
    PublicLimitedCompany = 6,

    [Description("Sole Proprietorship")]
    SoleProprietorship = 7,

    [Description("Non Governmental Organisation")]
    NGO = 8,

    [Description("Non Governmental Organisation")]
    Individual = 9
}
