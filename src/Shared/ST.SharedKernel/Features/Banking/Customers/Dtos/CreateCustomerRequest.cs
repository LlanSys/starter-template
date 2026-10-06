using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Features.Banking.Customers.Dtos;

public record CreateCustomerRequest(

    // ── Classification ──────────────────────────────────────────────────
    string Type,
    string SegmentType,
    string SubSegmentType,
    string? Classification,

    // ── Corporate Details ────────────────────────────────────────────────
    string CompanyName,
    string LineOfBusiness,
    string? LineOfBusinessMoreInfo,
    string NatureOfBusiness,
    string IdentificationType,
    string RegistrationNumber,
    DateTimeOffset DateOfRegistration,
    string? RegisteredAt,
    string? RegisteredOffice,
    int? BusinessStartedYear,
    int? NumberOfEmployees,
    string? Comments,
    string? Website,
    string? TINNumber,

    // ── Relationship Manager & Opening ──────────────────────────────────
    Guid RelationshipManagerId,
    DateTimeOffset OpenedOn,

    // ── Address ─────────────────────────────────────────────────────────
    string ResidentialAddress,
    string Country,
    string Region,
    string Ward,
    string District,
    string? BusinessAddress,
    string? OfficeAddress,
    string? MailingAddress,
    string? Street,
    string? ZipCode,
    string? PhoneHome,
    string? PhoneWork,
    string? Mobile,
    string? FaxNo,
    string? LandMark,
    string? Email,

    // ── Communication Preferences ────────────────────────────────────────
    bool CanSendGreetings,
    bool CanSendAssociateSpecialOffer,
    bool CanSendOurSpecialOffers,
    bool StatementOnline,
    bool MobileAlert
);
