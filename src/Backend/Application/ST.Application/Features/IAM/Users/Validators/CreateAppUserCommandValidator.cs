using ST.Application.Features.IAM.Users.Commands;
using ST.SharedKernel.Validation.Features.IAM.Users.Validators;
using FluentValidation;

namespace ST.Application.Features.IAM.Users.Validators;

public sealed class CreateAppUserCommandValidator : AbstractValidator<CreateAppUserCommand>
{
    public CreateAppUserCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty().MaximumLength(450);
        RuleFor(command => command.Request).NotNull().SetValidator(new CreateAppUserRequestValidator());
    }
}
