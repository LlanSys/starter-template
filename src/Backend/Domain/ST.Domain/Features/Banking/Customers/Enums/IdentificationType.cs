using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ST.Domain.Features.Banking.Customers.Enums;

public enum IdentificationType
{
    [Description("Certificate of Incorporation")]
    CertificateOfIncorporation = 1,

    [Description("Tax Identification Number")]
    TIN = 2,

    [Description("Business License")]
    BusinessLicense = 3,

    [Description("Company Registration Certificate")]
    CompanyRegistrationCertificate = 4
}
