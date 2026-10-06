using ST.Application.Features.IAM.Users.Commands;
using FluentValidation;

namespace ST.Application.Features.IAM.Users.Validators;

public sealed class DeactivateAdminUserCommandValidator : AbstractValidator<DeactivateAdminUserCommand>
{
    public DeactivateAdminUserCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty().MaximumLength(450);
        RuleFor(command => command.DeactivatedBy).NotEmpty().MaximumLength(450);
        RuleFor(command => command.Request).NotNull();
        When(command => command.Request is not null, () =>
            RuleFor(command => command.Request.Reason).MaximumLength(500));
    }
}
