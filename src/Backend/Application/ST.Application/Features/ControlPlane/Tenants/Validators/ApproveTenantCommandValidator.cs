using FluentValidation;
using ST.Application.Features.ControlPlane.Tenants.Commands;

namespace ST.Application.Features.ControlPlane.Tenants.Validators;

public sealed class ApproveTenantCommandValidator : AbstractValidator<ApproveTenantCommand>
{
    public ApproveTenantCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.EvidenceReference)
            .NotEmpty()
            .MaximumLength(256)
            .When(command => !command.Retry);
    }
}
