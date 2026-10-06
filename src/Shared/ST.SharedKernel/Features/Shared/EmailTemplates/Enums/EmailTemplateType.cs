using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ST.SharedKernel.Features.Shared.EmailTemplates.Enums;

public enum EmailTemplateType
{
    [Description("Standard Welcome")]
    StandardWelcome,

    [Description("App User Created Email")]
    AppUserCreated,

    [Description("Tenant Welcome Email")]
    TenantWelcome,

    [Description("Tenant Password Reset Email")]
    TenantPasswordReset,

    [Description("Tenant Notification Email")]
    TenantNotification,







    [Description("Password Reset Request Email")]
    PasswordResetRequest,

    [Description("Password Reset Code Email")]
    PasswordResetCode,

    [Description("Password Reset Success Email")]
    PasswordResetSuccess,

    [Description("Security Settings Changed Email")]
    SecuritySettingsChanged





}
