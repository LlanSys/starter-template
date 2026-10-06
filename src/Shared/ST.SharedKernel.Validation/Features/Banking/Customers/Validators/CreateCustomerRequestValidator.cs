using ST.Domain.Features.Banking.Customers.Enums;
using ST.SharedKernel.Extensions;
using ST.SharedKernel.Validation.Validators.Common;
using ST.SharedKernel.Features.Banking.Customers.Dtos;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Validation.Features.Banking.Customers.Validators;

public class CreateCustomerRequestValidator : Validator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        // ── Classification ──────────────────────────────────────────────
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Customer type is required.")
            .Must(v => Enum.TryParse<CustomerType>(v, out _))
            .WithMessage("Please select a valid customer type.");

        RuleFor(x => x.SegmentType)
            .NotEmpty().WithMessage("Segment type is required.")
            .Must(v => Enum.TryParse<SegmentType>(v, out _))
            .WithMessage("Please select a valid segment type.");

        RuleFor(x => x.SubSegmentType)
            .NotEmpty().WithMessage("Sub-segment type is required.")
            .Must(v => Enum.TryParse<SubSegmentType>(v, out _))
            .WithMessage("Please select a valid sub-segment type.");

        // ── Corporate Details ───────────────────────────────────────────
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(300).WithMessage("Company name cannot exceed 300 characters.");

        RuleFor(x => x.LineOfBusiness)
            .NotEmpty().WithMessage("Line of business is required.")
            .Must(v => Enum.TryParse<LineOfBusiness>(v, out _))
            .WithMessage("Please select a valid line of business.");

        RuleFor(x => x.NatureOfBusiness)
            .NotEmpty().WithMessage("Nature of business is required.")
            .MaximumLength(500).WithMessage("Nature of business cannot exceed 500 characters.");

        RuleFor(x => x.IdentificationType)
           .NotEmpty().WithMessage("Identification type is required.")
           .Must(v => Enum.TryParse<IdentificationType>(v, out _))
           .WithMessage("Please select a valid identification type.");

        RuleFor(x => x.RegistrationNumber)
            .NotEmpty().WithMessage("Registration number is required.")
            .MaximumLength(100).WithMessage("Registration number cannot exceed 100 characters.");

        RuleFor(x => x.DateOfRegistration)
            .NotEmpty().WithMessage("Date of registration is required.")
            .LessThanOrEqualTo(DateTime.Today)
            .WithMessage("Date of registration cannot be in the future.");

        RuleFor(x => x.BusinessStartedYear)
            .InclusiveBetween(1800, DateTime.Today.Year)
            .WithMessage($"Business started year must be between 1800 and {DateTime.Today.Year}.")
            .When(x => x.BusinessStartedYear.HasValue);

        RuleFor(x => x.NumberOfEmployees)
            .GreaterThan(0).WithMessage("Number of employees must be greater than 0.")
            .When(x => x.NumberOfEmployees.HasValue);

        RuleFor(x => x.Website)
            .MaximumLength(300).WithMessage("Website URL cannot exceed 300 characters.")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Website must be a valid URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.Website));

        RuleFor(x => x.TINNumber)
            .NotEmpty().WithMessage("TIN Number is required for corporate customers.")
            .MaximumLength(50).WithMessage("TIN Number cannot exceed 50 characters.")
            .When(x => x.Type == CustomerType.Enterprise.ToDisplayString());

        // ── Relationship Manager & Dates ────────────────────────────────
        RuleFor(x => x.RelationshipManagerId)
            .NotEmpty().WithMessage("Relationship Manager is required.");

        RuleFor(x => x.OpenedOn)
            .NotEmpty().WithMessage("Opened On date is required.")
            .LessThanOrEqualTo(DateTime.Today.AddDays(1))
            .WithMessage("Opened On date cannot be in the future.");

        // ── Address ─────────────────────────────────────────────────────
        RuleFor(x => x.ResidentialAddress)
            .NotEmpty().WithMessage("Residential address is required.")
            .MaximumLength(500).WithMessage("Residential address cannot exceed 500 characters.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100).WithMessage("Country cannot exceed 100 characters.");

        RuleFor(x => x.Region)
            .NotEmpty().WithMessage("Region is required.")
            .MaximumLength(100).WithMessage("Region cannot exceed 100 characters.");

        RuleFor(x => x.Ward)
            .NotEmpty().WithMessage("Ward is required.")
            .MaximumLength(100).WithMessage("Ward cannot exceed 100 characters.");

        RuleFor(x => x.District)
            .NotEmpty().WithMessage("District is required.")
            .MaximumLength(100).WithMessage("District cannot exceed 100 characters.");

        RuleFor(x => x.Mobile)
            .MaximumLength(30).WithMessage("Mobile number cannot exceed 30 characters.")
            .Matches(@"^\+?[\d\s\-\(\)]+$").WithMessage("Mobile number format is invalid.")
            .When(x => !string.IsNullOrWhiteSpace(x.Mobile));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Please enter a valid email address.")
            .MaximumLength(200).WithMessage("Email address cannot exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
