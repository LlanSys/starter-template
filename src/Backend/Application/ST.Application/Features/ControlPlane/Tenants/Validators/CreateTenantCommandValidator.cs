using FluentValidation;
using ST.Application.Features.ControlPlane.Tenants.Commands;
using System.Text.RegularExpressions;

namespace ST.Application.Features.ControlPlane.Tenants.Validators;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    private static readonly Regex SlugRegex = new("^[a-z0-9-]+$", RegexOptions.Compiled);

    public CreateTenantCommandValidator()
    {
        RuleFor(v => v.Request.Identifier)
            .NotEmpty().WithMessage("Identifier is required.")
            .MaximumLength(128).WithMessage("Identifier must not exceed 128 characters.")
            .Matches(SlugRegex).WithMessage("Identifier must be URL-safe (lowercase letters, numbers, and hyphens only).");

        RuleFor(v => v.Request.DisplayName)
            .NotEmpty().WithMessage("Display Name is required.")
            .MaximumLength(256).WithMessage("Display Name must not exceed 256 characters.");

        RuleFor(v => v.Request.HostName)
            .NotEmpty().WithMessage("Host Name is required.")
            .MaximumLength(256).WithMessage("Host Name must not exceed 256 characters.");

        RuleFor(v => v.Request.ContactEmail)
            .MaximumLength(256).WithMessage("Contact Email must not exceed 256 characters.")
            .EmailAddress().WithMessage("Contact Email must be a valid email address.")
            .When(v => !string.IsNullOrEmpty(v.Request.ContactEmail));

        When(command => command.Request.InitialAdministrator is not null, () =>
        {
            RuleFor(command => command.Request.InitialAdministrator!.AdministratorEmail).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(command => command.Request.InitialAdministrator!.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(command => command.Request.InitialAdministrator!.LastName).NotEmpty().MaximumLength(100);
        });

        RuleFor(v => v.Request.DeploymentStampId)
            .NotEmpty().WithMessage("Deployment Stamp ID is required.");

        RuleFor(v => v.Request.MaxUsers)
            .GreaterThanOrEqualTo(0).WithMessage("Max Users must be a non-negative number.");

        RuleFor(v => v.Request.DatabaseProvider)
            .MaximumLength(64).WithMessage("Database Provider must not exceed 64 characters.");

        RuleFor(v => v.Request.DatabaseConnectionString)
            .MaximumLength(2048).WithMessage("Database Connection String must not exceed 2048 characters.");
    }
}
