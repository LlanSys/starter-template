using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Features.IAM.Users.Enums;

public enum OtpPurpose
{
    Login = 1,
    EmailConfirmation = 2,
    PasswordReset = 3
}
