using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ST.Domain.Features.IAM.Users.Enums;

public enum MfaChannel
{
    [Description("None")]
    None = 0,

    [Description("SMS")]
    Sms = 1,

    [Description("Email Code")]
    EmailCode = 2,

    [Description("Email Link")]
    EmailLink = 3,

    [Description("Email and SMS")]
    EmailAndSms = 4,

    [Description("Authenticator App")]
    AuthenticatorApp = 5,
}
