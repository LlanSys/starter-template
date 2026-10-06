
using ST.SharedKernel.Validation.Validators.Common;
using ST.SharedKernel.Features.Banking.Customers.Dtos;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Validation.Features.Banking.Customers.Validators;

public class AddDirectorRequestValidator : Validator<AddDirectorRequest>
{
    public AddDirectorRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Director full name is required.")
            .MaximumLength(200).WithMessage("Full name cannot exceed 200 characters.");

        RuleFor(x => x.RelationType)
            .IsInEnum().WithMessage("Please select a valid relation type.");

        RuleFor(x => x.IdentificationType)
            .IsInEnum().WithMessage("Please select a valid identification type.");

        RuleFor(x => x.IdentificationNumber)
            .NotEmpty().WithMessage("Identification number is required.")
            .MaximumLength(100).WithMessage("Identification number cannot exceed 100 characters.");

        RuleFor(x => x.SharePercentage)
            .InclusiveBetween(0, 100).WithMessage("Share percentage must be between 0 and 100.")
            .When(x => x.SharePercentage.HasValue);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Please enter a valid email address.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(30).WithMessage("Phone number cannot exceed 30 characters.")
            .Matches(@"^\+?[\d\s\-\(\)]+$").WithMessage("Phone number format is invalid.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
    }
}
