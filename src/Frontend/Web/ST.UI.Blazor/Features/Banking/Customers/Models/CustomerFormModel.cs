using ST.SharedKernel.Features.Banking.Customers.Dtos;

namespace ST.UI.Blazor.Features.Banking.Customers.Models;

internal sealed class CustomerFormModel
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "Corporate";
    public string SegmentType { get; set; } = "Corporate";
    public string SubSegmentType { get; set; } = "Local";
    public string CompanyName { get; set; } = string.Empty;
    public string LineOfBusiness { get; set; } = "Financial Services";
    public string NatureOfBusiness { get; set; } = "Services";
    public string IdentificationType { get; set; } = "Certificate of Incorporation";
    public string RegistrationNumber { get; set; } = string.Empty;
    public DateTime? DateOfRegistration { get; set; } = DateTime.Today;
    public Guid RelationshipManagerId { get; set; }
    public DateTime? OpenedOn { get; set; } = DateTime.Today;
    public string ResidentialAddress { get; set; } = "Nairobi";
    public string Country { get; set; } = "Kenya";
    public string Region { get; set; } = "Nairobi";
    public string Ward { get; set; } = "Central";
    public string District { get; set; } = "Nairobi";
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? TinNumber { get; set; }
    public bool CanSendGreetings { get; set; } = true;
    public bool CanSendAssociateSpecialOffer { get; set; }
    public bool CanSendOurSpecialOffers { get; set; }
    public bool StatementOnline { get; set; } = true;
    public bool MobileAlert { get; set; } = true;

    public static CustomerFormModel From(CustomerResponse customer)
        => new()
        {
            Id = customer.Id,
            Type = customer.Type,
            SegmentType = customer.SegmentType,
            SubSegmentType = customer.SubSegmentType,
            CompanyName = customer.CompanyName,
            LineOfBusiness = customer.LineOfBusiness,
            NatureOfBusiness = customer.NatureOfBusiness ?? "Services",
            IdentificationType = customer.IdentificationType,
            RegistrationNumber = customer.RegistrationNumber,
            DateOfRegistration = customer.DateOfRegistration.Date,
            RelationshipManagerId = customer.RelationshipManagerId,
            OpenedOn = customer.OpenedOn.Date,
            ResidentialAddress = customer.ResidentialAddress ?? "Nairobi",
            Country = customer.Country ?? "Kenya",
            Region = customer.Region ?? "Nairobi",
            Ward = customer.Ward ?? "Central",
            District = customer.District ?? "Nairobi",
            Mobile = customer.Mobile,
            Email = customer.EmailId,
            TinNumber = customer.TINNumber,
            CanSendGreetings = customer.CanSendGreetings,
            CanSendAssociateSpecialOffer = customer.CanSendAssociateSpecialOffer,
            CanSendOurSpecialOffers = customer.CanSendOurSpecialOffers,
            StatementOnline = customer.StatementOnline,
            MobileAlert = customer.MobileAlert
        };

    public CreateCustomerRequest ToCreateRequest()
        => new(
            Type,
            SegmentType,
            SubSegmentType,
            null,
            CompanyName,
            LineOfBusiness,
            null,
            NatureOfBusiness,
            IdentificationType,
            RegistrationNumber,
            ToOffset(DateOfRegistration),
            null,
            null,
            null,
            null,
            null,
            null,
            TinNumber,
            RelationshipManagerId,
            ToOffset(OpenedOn),
            ResidentialAddress,
            Country,
            Region,
            Ward,
            District,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            Mobile,
            null,
            null,
            Email,
            CanSendGreetings,
            CanSendAssociateSpecialOffer,
            CanSendOurSpecialOffers,
            StatementOnline,
            MobileAlert);

    public UpdateCustomerRequest ToUpdateRequest()
        => new(
            Id,
            Type,
            SegmentType,
            SubSegmentType,
            null,
            CompanyName,
            LineOfBusiness,
            null,
            NatureOfBusiness,
            IdentificationType,
            RegistrationNumber,
            ToOffset(DateOfRegistration),
            null,
            null,
            null,
            null,
            null,
            null,
            TinNumber,
            RelationshipManagerId,
            ToOffset(OpenedOn),
            ResidentialAddress,
            Country,
            Region,
            Ward,
            District,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            Mobile,
            null,
            null,
            Email,
            CanSendGreetings,
            CanSendAssociateSpecialOffer,
            CanSendOurSpecialOffers,
            StatementOnline,
            MobileAlert);

    private static DateTimeOffset ToOffset(DateTime? value)
        => new(value ?? DateTime.Today, TimeSpan.Zero);
}
