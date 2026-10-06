using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ST.Domain.Features.HR.Employees.Enums;

public enum Gender
{
    [Description("Male")]
    Male = 0,

    [Description("Female")]
    Female = 1,

    [Description("Other")]
    Other = 2
}
