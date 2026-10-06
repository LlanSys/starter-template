using ST.Application.Features.IAM.Users.Commands;
using ST.SharedKernel.Validation.Features.IAM.Users.Validators;
using FluentValidation;

namespace ST.Application.Features.IAM.Users.Validators;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(new ChangePasswordRequestValidator());
    }
}
