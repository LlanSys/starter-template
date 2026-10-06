using ST.SharedKernel.Features.HR.Employees.Dtos;
using ST.SharedKernel.Features.Shared.Phone;
using ST.SharedKernel.Validation.Validators.Common;
using FluentValidation;

namespace ST.SharedKernel.Validation.Features.HR.Employees.Validators;

public sealed class CreateEmployeeRequestValidator : Validator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.IdNumber)
            .NotEmpty().WithMessage("ID number is required.")
            .MaximumLength(50).WithMessage("ID number cannot exceed 50 characters.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Please enter a valid email address.")
            .MaximumLength(200).WithMessage("Email cannot exceed 200 characters.");

        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Department is required.");

        RuleFor(x => x.PositionId)
            .NotEmpty().WithMessage("Position is required.");

        RuleFor(x => x.HireDate)
            .NotEmpty().WithMessage("Hire date is required.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today.AddDays(1)))
            .WithMessage("Hire date cannot be in the future.");

        RuleFor(x => x)
            .Must(HaveValidPhoneNumber)
            .WithMessage("Phone number is not valid for the selected country.");
    }

    private static bool HaveValidPhoneNumber(CreateEmployeeRequest request)
    {
        try
        {
            PhoneNumberFormatter.Normalize(request.CountryCode, request.PhoneNationalNumber, request.PhoneNumber);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
