using ST.Domain.Features.Banking.Customers.Enums;
using ST.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Features.Banking.Customers.ValueObjects;

/// <summary>
/// Owned entity — holds all corporate-specific details for an enterprise client.
/// </summary>
public class CorporateDetail
{
    public string CompanyName { get; private set; } = string.Empty;
    public LineOfBusiness LineOfBusiness { get; private set; }
    public string? LineOfBusinessMoreInfo { get; private set; }
    public string? NatureOfBusiness { get; private set; }
    public IdentificationType IdentificationType { get; private set; }
    public string RegistrationNumber { get; private set; } = string.Empty;
    public DateTimeOffset DateOfRegistration { get; private set; }
    public string? RegisteredAt { get; private set; }
    public string? RegisteredOffice { get; private set; }
    public int? BusinessStartedYear { get; private set; }
    public int? NumberOfEmployees { get; private set; }
    public string? Comments { get; private set; }
    public string? Website { get; private set; }
    public string? TINNumber { get; private set; }
    public string? Classification { get; private set; }

    private CorporateDetail() { }

    public static CorporateDetail Create(
        string companyName,
        LineOfBusiness lob,
        string natureOfBusiness,
        IdentificationType idType,
        string regNo,
        DateTimeOffset regDate,
        string? lobMoreInfo,
        string? registeredAt,
        string? registeredOffice,
        int? startedYear,
        int? employees,
        string? website,
        string? tin,
        string? classification,
        string? comments)
    {
        ArgumentNullException.ThrowIfNull(companyName);
        ArgumentNullException.ThrowIfNull(regNo);
        ArgumentNullException.ThrowIfNull(natureOfBusiness);

        return new CorporateDetail
        {
            CompanyName = companyName.Trim(),
            LineOfBusiness = lob,
            IdentificationType = idType,
            RegistrationNumber = regNo.Trim(),
            DateOfRegistration = regDate,
            LineOfBusinessMoreInfo = lobMoreInfo?.Trim(),
            NatureOfBusiness = natureOfBusiness?.Trim(),
            RegisteredAt = registeredAt?.Trim(),
            RegisteredOffice = registeredOffice?.Trim(),
            BusinessStartedYear = startedYear,
            NumberOfEmployees = employees,
            Website = website?.Trim(),
            TINNumber = tin?.Trim(),
            Classification = classification?.Trim(),
            Comments = comments?.Trim()
        };
    }

    public void Update(
        string companyName,
        LineOfBusiness lineOfBusiness,
        IdentificationType identificationType,
        string registrationNumber,
        DateTime dateOfRegistration,
        string? lineOfBusinessMoreInfo = null,
        string? natureOfBusiness = null,
        string? registeredAt = null,
        string? registeredOffice = null,
        int? businessStartedYear = null,
        int? numberOfEmployees = null,
        string? comments = null,
        string? website = null,
        string? tinNumber = null,
        string? clientClassification = null)
    {
        ArgumentNullException.ThrowIfNull(companyName);
        ArgumentNullException.ThrowIfNull(registrationNumber);

        CompanyName = companyName.Trim();
        LineOfBusiness = lineOfBusiness;
        IdentificationType = identificationType;
        RegistrationNumber = registrationNumber.Trim();
        DateOfRegistration = dateOfRegistration.Date;
        LineOfBusinessMoreInfo = lineOfBusinessMoreInfo?.Trim();
        NatureOfBusiness = natureOfBusiness?.Trim();
        RegisteredAt = registeredAt?.Trim();
        RegisteredOffice = registeredOffice?.Trim();
        BusinessStartedYear = businessStartedYear;
        NumberOfEmployees = numberOfEmployees;
        Comments = comments?.Trim();
        Website = website?.Trim();
        TINNumber = tinNumber?.Trim();
        Classification = clientClassification?.Trim();
    }
}
