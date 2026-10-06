using ST.SharedKernel.Features.IAM.Permissions.Dtos;
using ST.SharedKernel.Validation.Validators.Common;
using FluentValidation;

namespace ST.SharedKernel.Validation.Features.IAM.Permissions.Validators;

public sealed class CreatePermissionRequestValidator : Validator<CreatePermissionRequest>
{
    public CreatePermissionRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Context)
            .NotEmpty().WithMessage("Permission context is required.")
            .MaximumLength(80).WithMessage("Permission context cannot exceed 80 characters.")
            .Matches("^[A-Za-z][A-Za-z0-9_-]*$").WithMessage("Permission context must start with a letter and contain only letters, numbers, hyphens, or underscores.");

        RuleFor(x => x.Resource)
            .NotEmpty().WithMessage("Permission resource is required.")
            .MaximumLength(80).WithMessage("Permission resource cannot exceed 80 characters.")
            .Matches("^[A-Za-z][A-Za-z0-9_-]*$").WithMessage("Permission resource must start with a letter and contain only letters, numbers, hyphens, or underscores.");

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Permission action is required.")
            .MaximumLength(80).WithMessage("Permission action cannot exceed 80 characters.")
            .Matches("^[A-Za-z][A-Za-z0-9_-]*$").WithMessage("Permission action must start with a letter and contain only letters, numbers, hyphens, or underscores.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Permission description cannot exceed 500 characters.");
    }
}
