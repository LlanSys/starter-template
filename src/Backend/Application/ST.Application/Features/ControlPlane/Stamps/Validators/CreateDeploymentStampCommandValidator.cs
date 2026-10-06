using FluentValidation;
using ST.Application.Features.ControlPlane.Stamps.Commands;

namespace ST.Application.Features.ControlPlane.Stamps.Validators;

public class CreateDeploymentStampCommandValidator : AbstractValidator<CreateDeploymentStampCommand>
{
    public CreateDeploymentStampCommandValidator()
    {
        RuleFor(v => v.Request.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(256).WithMessage("Name must not exceed 256 characters.");

        RuleFor(v => v.Request.TargetResourceGroup)
            .NotEmpty().WithMessage("Target Resource Group is required.")
            .MaximumLength(256).WithMessage("Target Resource Group must not exceed 256 characters.");

        RuleFor(v => v.Request.KeyVaultUri)
            .MaximumLength(1024).WithMessage("Key Vault URI must not exceed 1024 characters.");

        RuleFor(v => v.Request.DatabaseProvider)
            .MaximumLength(64).WithMessage("Database Provider must not exceed 64 characters.");

        RuleFor(v => v.Request.DatabaseConnectionString)
            .MaximumLength(2048).WithMessage("Database Connection String must not exceed 2048 characters.");
    }
}
