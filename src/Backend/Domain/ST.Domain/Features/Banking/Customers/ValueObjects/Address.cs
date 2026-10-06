using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ST.Domain.Features.Banking.Customers.ValueObjects;

public class Address
{
    public string ResidentialAddress { get; private set; } = string.Empty;
    public string? BusinessAddress { get; private set; }
    public string? OfficeAddress { get; private set; }
    public string? MailingAddress { get; private set; }
    public string? HomeCountryAddress { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string Country { get; private set; } = string.Empty;
    public string Region { get; private set; } = string.Empty;
    public string Ward { get; private set; } = string.Empty;
    public string District { get; private set; } = string.Empty;
    public string? Street { get; private set; }
    public string? ZipCode { get; private set; }
    public string? PhoneHome { get; private set; }
    public string? PhoneWork { get; private set; }
    public string? Mobile { get; private set; }
    public string? FaxNo { get; private set; }
    public string? LandMark { get; private set; }
    public string? Email { get; private set; }

    // EF Core
    private Address() { }

    public static Address Create(
        string residentialAddress,
        string country,
        string region,
        string ward,
        string district,
        string? mobile = null,
        string? emailId = null,
        string? businessAddress = null,
        string? officeAddress = null,
        string? mailingAddress = null,
        string? homeCountryAddress = null,
        string? addressLine2 = null,
        string? street = null,
        string? zipCode = null,
        string? phoneHome = null,
        string? phoneWork = null,
        string? faxNo = null,
        string? landMark = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(residentialAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(country);
        ArgumentException.ThrowIfNullOrWhiteSpace(region);
        ArgumentException.ThrowIfNullOrWhiteSpace(ward);
        ArgumentException.ThrowIfNullOrWhiteSpace(district);

        return new Address
        {
            ResidentialAddress = residentialAddress.Trim(),
            Country = country.Trim(),
            Region = region.Trim(),
            Ward = ward.Trim(),
            District = district.Trim(),
            Mobile = mobile?.Trim(),
            Email = emailId?.Trim().ToLower(CultureInfo.CurrentCulture),
            BusinessAddress = businessAddress?.Trim(),
            OfficeAddress = officeAddress?.Trim(),
            MailingAddress = mailingAddress?.Trim(),
            HomeCountryAddress = homeCountryAddress?.Trim(),
            AddressLine2 = addressLine2?.Trim(),
            Street = street?.Trim(),
            ZipCode = zipCode?.Trim(),
            PhoneHome = phoneHome?.Trim(),
            PhoneWork = phoneWork?.Trim(),
            FaxNo = faxNo?.Trim(),
            LandMark = landMark?.Trim()
        };
    }

    internal void Update(
        string residentialAddress,
        string country,
        string region,
        string ward,
        string district,
        string? mobile = null,
        string? emailId = null,
        string? businessAddress = null,
        string? officeAddress = null,
        string? mailingAddress = null,
        string? street = null,
        string? zipCode = null,
        string? phoneHome = null,
        string? phoneWork = null,
        string? faxNo = null,
        string? landMark = null)
    {
        ResidentialAddress = residentialAddress.Trim();
        Country = country.Trim();
        Region = region.Trim();
        Ward = ward.Trim();
        District = district.Trim();
        Mobile = mobile?.Trim();
        Email = emailId?.Trim().ToLower(CultureInfo.CurrentCulture);
        BusinessAddress = businessAddress?.Trim();
        OfficeAddress = officeAddress?.Trim();
        MailingAddress = mailingAddress?.Trim();
        Street = street?.Trim();
        ZipCode = zipCode?.Trim();
        PhoneHome = phoneHome?.Trim();
        PhoneWork = phoneWork?.Trim();
        FaxNo = faxNo?.Trim();
        LandMark = landMark?.Trim();
    }
}
