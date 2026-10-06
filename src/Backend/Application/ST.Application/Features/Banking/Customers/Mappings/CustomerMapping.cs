using ST.SharedKernel.Extensions;
using ST.Domain.Features.Banking.Customers.Entities;
using ST.SharedKernel.Features.Banking.Customers.Dtos;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace ST.Application.Features.Banking.Customers.Mappings;

internal static class CustomerMapping
{
    public static CustomerResponse ToCustomerResponse(this Customer customer) =>
        new(
            // Identity & Classification
            customer.Id,
            customer.Number,
            customer.Type.ToDisplayString(),
            customer.SegmentType.ToDisplayString(),
            customer.SubSegmentType.ToDisplayString(),
            customer.Status.ToDisplayString(),
            customer.OpenedOn,

            // Corporate Details
            customer.CorporateDetail.CompanyName,
            customer.CorporateDetail.LineOfBusiness.ToDisplayString(),
            customer.CorporateDetail.LineOfBusinessMoreInfo,
            customer.CorporateDetail.NatureOfBusiness,
            customer.CorporateDetail.IdentificationType.ToDisplayString(),
            customer.CorporateDetail.RegistrationNumber,
            customer.CorporateDetail.DateOfRegistration,
            customer.CorporateDetail.RegisteredAt,
            customer.CorporateDetail.RegisteredOffice,
            customer.CorporateDetail.BusinessStartedYear,
            customer.CorporateDetail.NumberOfEmployees,
            customer.CorporateDetail.Comments,
            customer.CorporateDetail.Website,
            customer.CorporateDetail.TINNumber,

            // Relationship Manager
            customer.RelationshipManagerId,
            $"{customer.RelationshipManager?.FirstName} {customer.RelationshipManager?.LastName}" ?? "—",

            // Address
            customer.Address.ResidentialAddress,
            customer.Address.Country,
            customer.Address.Region,
            customer.Address.Ward,
            customer.Address.District,
            customer.Address.BusinessAddress,
            customer.Address.OfficeAddress,
            customer.Address.MailingAddress,
            customer.Address.Street,
            customer.Address.ZipCode,
            customer.Address.PhoneHome,
            customer.Address.PhoneWork,
            customer.Address.Mobile,
            customer.Address.FaxNo,
            customer.Address.LandMark,
            customer.Address.Email,

            // Communication Prefs
            customer.CommunicationPreference.CanSendGreetings,
            customer.CommunicationPreference.CanSendAssociateSpecialOffer,
            customer.CommunicationPreference.CanSendOurSpecialOffers,
            customer.CommunicationPreference.StatementOnline,
            customer.CommunicationPreference.MobileAlert,

            // Directors
            [.. customer.Directors.Select(d => d.ToDirectorResponse())]
        );

    public static DirectorResponse ToDirectorResponse(this Director director) =>
        new(
            director.Id,
            director.FullName,
            director.RelationType.ToDisplayString(),
            director.IdentificationType.ToDisplayString(),
            director.IdentificationNumber,
            director.PhoneNumber,
            director.Email,
            director.SharePercentage,
            director.CreatedAt
        );

    public static Expression<Func<Customer, CustomerResponse>> AsResponse => customer => new CustomerResponse(
        customer.Id,
        customer.Number,
        customer.Type.ToDisplayString(), // EF can translate simple ToString() or use a helper
        customer.SegmentType.ToDisplayString(),
        customer.SubSegmentType.ToDisplayString(),
        customer.Status.ToDisplayString(),
        customer.OpenedOn,

        // Corporate Details
        customer.CorporateDetail.CompanyName,
        customer.CorporateDetail.LineOfBusiness.ToDisplayString(),
        customer.CorporateDetail.LineOfBusinessMoreInfo,
        customer.CorporateDetail.NatureOfBusiness,
        customer.CorporateDetail.IdentificationType.ToDisplayString(),
        customer.CorporateDetail.RegistrationNumber,
        customer.CorporateDetail.DateOfRegistration,
        customer.CorporateDetail.RegisteredAt,
        customer.CorporateDetail.RegisteredOffice,
        customer.CorporateDetail.BusinessStartedYear,
        customer.CorporateDetail.NumberOfEmployees,
        customer.CorporateDetail.Comments,
        customer.CorporateDetail.Website,
        customer.CorporateDetail.TINNumber,
        customer.RelationshipManagerId,
        customer.RelationshipManager != null ? $"{customer.RelationshipManager.FirstName} {customer.RelationshipManager.LastName}" : "—",

        // Address
        customer.Address.ResidentialAddress,
        customer.Address.Country,
        customer.Address.Region,
        customer.Address.Ward,
        customer.Address.District,
        customer.Address.BusinessAddress,
        customer.Address.OfficeAddress,
        customer.Address.MailingAddress,
        customer.Address.Street,
        customer.Address.ZipCode,
        customer.Address.PhoneHome,
        customer.Address.PhoneWork,
        customer.Address.Mobile,
        customer.Address.FaxNo,
        customer.Address.LandMark,
        customer.Address.Email,

        // Communication Prefs
        customer.CommunicationPreference.CanSendGreetings,
        customer.CommunicationPreference.CanSendAssociateSpecialOffer,
        customer.CommunicationPreference.CanSendOurSpecialOffers,
        customer.CommunicationPreference.StatementOnline,
        customer.CommunicationPreference.MobileAlert,

        customer.Directors.Select(d => new DirectorResponse(
            d.Id,
            d.FullName,
            d.RelationType.ToDisplayString(),
            d.IdentificationType.ToDisplayString(),
            d.IdentificationNumber,
            d.PhoneNumber,
            d.Email,
            d.SharePercentage,
            d.CreatedAt))

        .ToList()
    );


}
