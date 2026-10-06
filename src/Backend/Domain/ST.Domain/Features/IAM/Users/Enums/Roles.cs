using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ST.Domain.Features.IAM.Users.Enums;

public enum Roles
{
    [Description("System Administrator")]
    SysAdmin,

    [Description("Employee")]
    Employee,

    [Description("Customer")]
    Customer
}
