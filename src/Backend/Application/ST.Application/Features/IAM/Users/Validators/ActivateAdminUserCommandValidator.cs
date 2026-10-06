using ST.Application.Features.IAM.Users.Commands;
using FluentValidation;

namespace ST.Application.Features.IAM.Users.Validators;

public sealed class ActivateAdminUserCommandValidator : AbstractValidator<ActivateAdminUserCommand>
{
    public ActivateAdminUserCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty().MaximumLength(450);
        RuleFor(command => command.ActivatedBy).NotEmpty().MaximumLength(450);
    }
}
