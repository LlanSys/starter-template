using ST.SharedKernel.Features.HR.Departments.Dtos;
using ST.SharedKernel.Validation.Validators.Common;
using FluentValidation;

namespace ST.SharedKernel.Validation.Features.HR.Departments.Validators;

public sealed class CreateDepartmentRequestValidator : Validator<CreateDepartmentRequest>
{
    public CreateDepartmentRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Department code is required.")
            .MaximumLength(20).WithMessage("Department code cannot exceed 20 characters.")
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("Department code may only contain letters, numbers, hyphens, and underscores.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Department name is required.")
            .MaximumLength(120).WithMessage("Department name cannot exceed 120 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Department description cannot exceed 500 characters.");
    }
}
