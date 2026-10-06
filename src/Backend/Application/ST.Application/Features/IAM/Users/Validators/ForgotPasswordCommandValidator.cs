using ST.Application.Features.IAM.Users.Commands;
using ST.SharedKernel.Validation.Features.IAM.Users.Validators;
using FluentValidation;

namespace ST.Application.Features.IAM.Users.Validators;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(new ForgotPasswordRequestValidator());
    }
}
