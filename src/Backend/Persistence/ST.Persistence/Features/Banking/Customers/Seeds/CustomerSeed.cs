using ST.Domain.Features.Banking.Customers.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Persistence.Features.Banking.Customers.Seeds;

/// <summary>
/// Provides deterministic seed data for the <see cref="ST.Domain.Features.Banking.Customers.Entities.Customer"/> aggregate
/// and all its owned entities (<see cref="CorporateDetail"/>, <see cref="Address"/>,
/// <see cref="CommunicationPreference"/>).
/// </summary>
/// <remarks>
/// <para>
/// <b>Migration stability:</b> All IDs and timestamps are fixed literals. Do not replace them
/// with <c>Guid.CreateVersion7()</c> or <c>DateTimeOffset.UtcNow</c> — doing so causes EF Core
/// to generate a spurious <c>UpdateData</c> migration on every <c>dotnet ef migrations add</c>.
/// </para>
/// <para>
/// <b>Owned entity seeding:</b> <see cref="CorporateDetail"/>, <see cref="Address"/>, and
/// <see cref="CommunicationPreference"/> are EF Core owned entities. Their seed data is exposed
/// as anonymous objects that include the <c>CustomerId</c> shadow FK property.
/// Call these methods from within <c>IEntityTypeConfiguration&lt;Customer&gt;</c>:
/// <code>
/// entity.OwnsOne(c => c.CorporateDetail,   cd => cd.HasData(CustomerSeed.GetCorporateDetailSeedData()));
/// entity.OwnsOne(c => c.Address,           a  => a.HasData(CustomerSeed.GetAddressSeedData()));
/// entity.OwnsOne(c => c.CommunicationPreference, cp => cp.HasData(CustomerSeed.GetCommunicationPreferenceSeedData()));
/// </code>
/// </para>
/// <para>
/// <b>RelationshipManagerId values</b> reference the GUIDs defined in <see cref="EmployeeSeed"/>.
/// That seed must be applied before or alongside this one (EF handles ordering via FK constraints).
/// </para>
/// </remarks>
public static class CustomerSeed
{
    // -------------------------------------------------------------------------
    // Fixed reference values — must never change after the first migration
    // -------------------------------------------------------------------------

    private static readonly DateTimeOffset SeedDate = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = new("0194f700-0000-7000-8000-000000000001");

    // Employee GUIDs from EmployeeSeed — referenced as RelationshipManagerId
    private static readonly Guid[] RmIds =
    [
        new("0194f800-0000-7000-8000-000000000001"), // Alex Odhiambo
        new("0194f800-0000-7000-8000-000000000002"), // Allan Alex
        new("0194f800-0000-7000-8000-000000000003"), // Laura Omito
    ];

    // 150 pre-generated, fixed customer GUIDs.
    // Pattern: 0194f900-0000-7000-8000-{1-based index padded to 12 decimal digits}
    // Generated once; treat as immutable after the initial migration.
    public static readonly Guid[] CustomerIds = Enumerable
        .Range(1, 150)
        .Select(i => new Guid($"0194f900-0000-7000-8000-{i:D12}"))
        .ToArray();

    // -------------------------------------------------------------------------
    // Source data pools
    // -------------------------------------------------------------------------

    private static readonly string[] CompanyNames =
    [
        "Safaricom", "KCB Group", "Equity Group", "East African Breweries",
        "Nation Media Group", "Bamburi Cement", "Britam Holdings", "Centum Investment",
        "Co-operative Bank", "Diamond Trust Bank", "NCBA Group", "Stanbic Bank Kenya",
        "Absa Bank Kenya", "I&M Holdings", "Jubilee Holdings", "CIC Insurance Group",
        "Sanlam Kenya", "Kenya Airways", "Kenya Power", "Kenol Kobil",
        "TotalEnergies Kenya", "Twiga Foods", "Copia Kenya", "M-Kopa Solar",
        "Cellulant Corporation", "Africa's Talking", "Pesalink", "Lipa Na Mpesa",
        "WPP Scangroup", "Longhorn Publishers", "Uchumi Supermarkets", "Naivas",
        "Carrefour Kenya", "Java House", "Artcaffe", "Chicken Inn Kenya",
        "Galana Oil", "Davis & Shirtliff", "Devki Group", "Bidco Africa",
        "Kevian Kenya", "Unga Group", "Kakuzi", "Williamson Tea",
        "Kapchorua Tea", "Eastern Produce", "Sameer Africa", "CMC Motors",
        "Car & General", "Cooper Motor Corporation", "Automark Kenya",
        "Isuzu East Africa", "Toyota Kenya", "DT Dobie", "Simba Colt Motors",
        "Civicon Engineering", "Nyoro Construction", "China Wu Yi Kenya",
        "Roko Construction", "Engineering Solutions EA", "Alphacom Systems",
        "Oracle Kenya", "Microsoft Kenya", "IBM East Africa", "Liquid Telecom",
        "Seacom Kenya", "Airtel Kenya", "Telkom Kenya", "Faulu Bank",
        "KWFT Microfinance", "Rafiki Microfinance", "Century Microfinance",
        "Letshego Kenya", "Watu Credit", "Mwananchi Credit", "Tala Kenya",
        "Branch International", "Zenka Finance", "Okolea International",
        "Accenture Kenya", "Deloitte East Africa", "KPMG Kenya", "PwC Kenya",
        "EY Kenya", "Grant Thornton Kenya", "BDO East Africa", "Bowmans Kenya",
        "Anjarwalla & Khanna", "Coulson Harney", "Walker Kontos Advocates",
        "Hamilton Harrison & Mathews", "Ashitiva Advocates", "Mboya Wangong'u",
        "G4S Kenya", "Securex Agencies", "Wells Fargo Security", "KK Security",
        "Radar Security Systems", "Canon Kenya", "Ricoh East Africa",
        "Hewlett Packard Kenya", "Dell Technologies Kenya", "Lenovo Kenya",
        "Philips East Africa", "Siemens Kenya", "ABB Kenya", "Schneider Electric EA",
        "Honeywell Kenya", "Grundfos Kenya", "Atlas Copco Kenya", "Kaeser Kenya",
        "Scania East Africa", "MAN Trucks Kenya", "Volvo Group Kenya",
        "Caterpillar Kenya", "Komatsu East Africa", "John Deere Kenya",
        "Cargill Kenya", "Louis Dreyfus Kenya", "Olam Kenya",
        "Advanta Seeds", "SeedCo Kenya", "Western Seed Company",
        "Syngenta Kenya", "BASF Kenya", "Bayer Crop Science EA",
        "Elgon Kenya", "Sunripe", "Flamingo Horticulture",
        "Oserian Flowers", "Sian Flowers", "Wildfire Flowers",
        "Nairobi Java House", "Sarova Hotels", "Serena Hotels EA",
        "Pride Inn Hotels", "Tribe Hotel Nairobi", "Hemingways Kenya",
        "Fairmont Hotels EA", "DHL Kenya", "FedEx Kenya", "UPS Kenya",
        "Siginon Freight", "Kenya National Shipping Line", "Bollore Logistics"
    ];

    private static readonly string[] Regions =
    [
        "Nairobi", "Mombasa", "Kisumu", "Nakuru", "Eldoret",
        "Nyeri", "Meru", "Thika", "Kikuyu", "Machakos",
        "Kitale", "Garissa", "Kisii", "Kericho", "Bungoma"
    ];

    private static readonly string[] Districts =
    [
        "Westlands", "Karen", "Kilimani", "Upperhill", "Parklands",
        "Industrial Area", "South C", "South B", "Lavington", "Kileleshwa",
        "Tudor", "Nyali", "Likoni", "Kisauni", "Bamburi",
        "Milimani", "Lanet", "Bahati", "Menengai", "Bondeni"
    ];

    private static readonly string[] Wards =
    [
        "Parklands/Highridge", "Kitisuru", "Lavington", "Kilimani",
        "Karura", "Kangemi", "Mountain View", "Waithaka", "Dagoretti",
        "Woodley/Kenyatta", "Ngumo", "Karen", "Nairobi West",
        "South B", "Harambee", "Starehe", "Ngara"
    ];

    private static readonly string[] Streets =
    [
        "Mama Ngina Street", "Kenyatta Avenue", "Moi Avenue",
        "Haile Selassie Avenue", "Uhuru Highway", "Waiyaki Way",
        "Ngong Road", "Langata Road", "Thika Road", "Mombasa Road",
        "Ring Road Westlands", "Museum Hill Road", "Upper Hill Road",
        "Riverside Drive", "Valley Road", "Dennis Pritt Road"
    ];

    private static readonly (LineOfBusiness Lob, string[] Natures)[] NaturesByLob =
    [
        (LineOfBusiness.Technology, ["Software Development", "IT Consulting", "Cloud Services", "Cybersecurity Solutions", "Fintech Services"]),
        (LineOfBusiness.FinancialServices, ["Banking & Lending", "Insurance Underwriting", "Investment Management", "Microfinance", "Payment Processing"]),

        (LineOfBusiness.Manufacturing, ["Fast Moving Consumer Goods", "Beverage Production", "Cement Manufacturing", "Packaging", "Steel Fabrication"]),

        (LineOfBusiness.Agriculture, ["Flower Farming", "Tea Farming", "Grain Trading", "Livestock Rearing", "Horticulture Export"]),
        (LineOfBusiness.Retail, ["Supermarket Chain", "Electronics Retail", "Fashion Retail", "Hardware & Building Materials", "Pharmacy"]),
        (LineOfBusiness.Services, ["Professional Consulting", "Security Services", "Hospitality", "Logistics & Freight", "Legal Services"]),
        (LineOfBusiness.Trading, ["Import & Export", "Commodity Trading", "Motor Vehicle Distribution", "Fuel Distribution", "Medical Supplies"]),

        (LineOfBusiness.Proprietary, ["Proprietary Technology", "Brand Licensing", "Intellectual Property", "Franchise Operations", "Patent Holdings"]),
    ];

    private static readonly (CustomerType Type, SegmentType Segment, SubSegmentType SubSegment)[] SegmentMatrix =
    [
        (CustomerType.Corporate,             SegmentType.Corporate,             SubSegmentType.Local),
        (CustomerType.Corporate,             SegmentType.Corporate,             SubSegmentType.Multinational),
        (CustomerType.Corporate,             SegmentType.Corporate,             SubSegmentType.GovernmentOwned),
        (CustomerType.Corporate,             SegmentType.PublicLimitedCompany,  SubSegmentType.PublicLimitedCompany),
        (CustomerType.Corporate,             SegmentType.PrivateLimitedCompany, SubSegmentType.PrivateLimitedCompany),
        (CustomerType.Institutional,         SegmentType.Corporate,             SubSegmentType.NGO),
        (CustomerType.SmallMediumEnterprise, SegmentType.SME,                   SubSegmentType.Partnership),
        (CustomerType.SmallMediumEnterprise, SegmentType.SME,                   SubSegmentType.SoleProprietorship),
        (CustomerType.Enterprise,            SegmentType.PublicLimitedCompany,  SubSegmentType.Multinational),
        (CustomerType.Enterprise,            SegmentType.PrivateLimitedCompany, SubSegmentType.Local),
    ];

    private static readonly IdentificationType[] IdTypes =
    [
        IdentificationType.CertificateOfIncorporation,
        IdentificationType.CompanyRegistrationCertificate,
        IdentificationType.BusinessLicense,
        IdentificationType.TIN
    ];

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the 150 <see cref="ST.Domain.Features.Banking.Customers.Entities.Customer"/> root records as anonymous objects
    /// for use with <c>entity.HasData(...)</c> inside
    /// <c>IEntityTypeConfiguration&lt;Customer&gt;</c>.
    /// </summary>
    /// <remarks>
    /// We return <c>object</c> (anonymous type) rather than <c>Customer</c> because
    /// <c>HasData</c> for owned entities and shadow properties requires the anonymous
    /// object form. For the customer root itself this isn't strictly necessary, but using
    /// a consistent pattern across all seed methods avoids confusion.
    /// </remarks>
    public static IEnumerable<object> GetCustomerSeedData()
    {
        var r = new Random(42); // Fixed seed — deterministic across all runs

        return Enumerable.Range(0, 150).Select(i =>
        {
            var seg = SegmentMatrix[i % SegmentMatrix.Length];

            return (object)new
            {
                Id = CustomerIds[i],
                Number = $"CUS-{(i + 1):D4}",
                Name = $"{CompanyNames[i % CompanyNames.Length]} {GetSuffix(seg.Type)}",
                Type = seg.Type,
                SegmentType = seg.Segment,
                SubSegmentType = seg.SubSegment,
                Status = CustomerStatus.Draft,
                OpenedOn = SeedDate,
                RelationshipManagerId = RmIds[i % RmIds.Length],
                TenantId,
                IsDeleted = false,
                DeletedAt = (DateTimeOffset?)null,
                DeletedBy = (string?)null,
                CreatedAt = SeedDate,
                CreatedBy = "System",
                UpdatedAt = (DateTimeOffset?)null,
                UpdatedBy = (string?)null,
                RowVersion = Array.Empty<byte>(),
            };
        });
    }

    /// <summary>
    /// Returns seed data for the owned <see cref="CorporateDetail"/> entity.
    /// Use inside <c>entity.OwnsOne(c => c.CorporateDetail, cd => cd.HasData(...))</c>.
    /// The <c>CustomerId</c> property is the shadow FK linking back to the owning customer.
    /// </summary>
    public static IEnumerable<object> GetCorporateDetailSeedData()
    {
        var r = new Random(42);

        return Enumerable.Range(0, 150).Select(i =>
        {
            var (lob, natures) = NaturesByLob[i % NaturesByLob.Length];
            var nature = natures[i % natures.Length];
            var regYear = 2000 + (i % 24);          // 2000 – 2023
            var regDate = new DateTimeOffset(regYear, 1 + (i % 12), 1, 0, 0, 0, TimeSpan.Zero);
            var companyName = $"{CompanyNames[i % CompanyNames.Length]} {GetSuffix(SegmentMatrix[i % SegmentMatrix.Length].Type)}";

            return (object)new
            {
                CustomerId = CustomerIds[i],        // shadow FK — required
                CompanyName = companyName,
                LineOfBusiness = lob,
                LineOfBusinessMoreInfo = (string?)null,
                NatureOfBusiness = nature,
                IdentificationType = IdTypes[i % IdTypes.Length],
                RegistrationNumber = $"CPR/{(i + 1):D6}/{regYear}",
                DateOfRegistration = regDate,
                RegisteredAt = "Companies Registry, Nairobi",
                RegisteredOffice = $"{CompanyNames[i % CompanyNames.Length]} House, {Streets[i % Streets.Length]}",
                BusinessStartedYear = regYear + 1,
                NumberOfEmployees = 50 + (i * 7 % 950),
                Comments = (string?)null,
                Website = $"https://www.{CompanyNames[i % CompanyNames.Length].ToUpperInvariant()
                .Replace(" ", "", StringComparison.Ordinal)
                .Replace("&", "and", StringComparison.Ordinal)}.co.ke",

                TINNumber = $"P0{(51234560 + i):D8}Y",
                Classification = GetClassification(i),
            };
        });
    }

    /// <summary>
    /// Returns seed data for the owned <see cref="Address"/> entity.
    /// Use inside <c>entity.OwnsOne(c => c.Address, a => a.HasData(...))</c>.
    /// </summary>
    public static IEnumerable<object> GetAddressSeedData()
    {
        return Enumerable.Range(0, 150).Select(i => (object)new
        {
            CustomerId = CustomerIds[i],
            ResidentialAddress = $"{CompanyNames[i % CompanyNames.Length]} Building, {Streets[i % Streets.Length]}",
            BusinessAddress = $"P.O. Box {1000 + i}, {Regions[i % Regions.Length]}",
            OfficeAddress = (string?)null,
            MailingAddress = (string?)null,
            HomeCountryAddress = (string?)null,
            AddressLine2 = (string?)null,
            Country = "Kenya",
            Region = Regions[i % Regions.Length],
            Ward = Wards[i % Wards.Length],
            District = Districts[i % Districts.Length],
            Street = Streets[i % Streets.Length],
            ZipCode = $"{(00100 + (i % 100)):D5}",
            PhoneHome = (string?)null,
            PhoneWork = $"+254 20 {(2000000 + i * 3):D7}",
            Mobile = $"+254 7{(10000000 + i * 7):D8}",
            FaxNo = (string?)null,
            LandMark = $"Near {Streets[(i + 1) % Streets.Length]}",
            EmailId = $"info@{CompanyNames[i % CompanyNames.Length].ToUpperInvariant()
            .Replace(" ", "", StringComparison.Ordinal)
            .Replace("&", "and", StringComparison.Ordinal)}.co.ke",
        });
    }

    /// <summary>
    /// Returns seed data for the owned <see cref="CommunicationPreference"/> entity.
    /// Use inside <c>entity.OwnsOne(c => c.CommunicationPreference, cp => cp.HasData(...))</c>.
    /// </summary>
    public static IEnumerable<object> GetCommunicationPreferenceSeedData()
    {
        return Enumerable.Range(0, 150).Select(i => (object)new
        {
            CustomerId = CustomerIds[i],
            CanSendGreetings = i % 2 == 0,
            CanSendAssociateSpecialOffer = i % 3 == 0,
            CanSendOurSpecialOffers = i % 4 == 0,
            StatementOnline = i % 2 != 0,
            MobileAlert = true,
        });
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static string GetSuffix(CustomerType type) => type switch
    {
        CustomerType.Corporate => "Ltd",
        CustomerType.Enterprise => "PLC",
        CustomerType.Institutional => "Organization",
        CustomerType.SmallMediumEnterprise => "SME",
        CustomerType.Individual => string.Empty,
        _ => "Ltd"
    };

    private static string GetClassification(int index) => (index % 4) switch
    {
        0 => "Gold",
        1 => "Silver",
        2 => "Platinum",
        _ => "Standard"
    };
}
