

using ST.SharedKernel.Validation.Validators.Common;
using ST.SharedKernel.Features.Banking.Customers.Dtos;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Validation.Features.Banking.Customers.Validators;

public class UpdateCustomerRequestValidator : Validator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Customer ID is required.");

        // Reuse all rules from Create — delegate to avoid duplication
        RuleFor(x => new CreateCustomerRequest(
            x.Type, x.SegmentType, x.SubSegmentType, x.Classification,
            x.CompanyName, x.LineOfBusiness, x.LineOfBusinessMoreInfo, x.NatureOfBusiness,
            x.IdentificationType, x.RegistrationNumber, x.DateOfRegistration,
            x.RegisteredAt, x.RegisteredOffice, x.BusinessStartedYear, x.NumberOfEmployees,
            x.Comments, x.Website, x.TINNumber,
            x.RelationshipManagerId, x.OpenedOn,
            x.ResidentialAddress, x.Country, x.Region, x.Ward, x.District,
            x.BusinessAddress, x.OfficeAddress, x.MailingAddress,
            x.Street, x.ZipCode, x.PhoneHome, x.PhoneWork, x.Mobile,
            x.FaxNo, x.LandMark, x.EmailId,
            x.CanSendGreetings, x.CanSendAssociateSpecialOffer,
            x.CanSendOurSpecialOffers, x.StatementOnline, x.MobileAlert))
            .SetValidator(new CreateCustomerRequestValidator());
    }
}
