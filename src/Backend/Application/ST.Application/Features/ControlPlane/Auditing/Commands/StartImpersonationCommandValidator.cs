using FluentValidation;

namespace ST.Application.Features.ControlPlane.Auditing.Commands;

public class StartImpersonationCommandValidator : AbstractValidator<StartImpersonationCommand>
{
    public StartImpersonationCommandValidator()
    {
        RuleFor(x => x.TargetTenantId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1024);
        RuleFor(x => x.DurationHours).GreaterThan(0).LessThanOrEqualTo(24);
    }
}
