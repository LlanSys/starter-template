using ST.Application.Features.IAM.Menus.Commands;
using ST.SharedKernel.Validation.Features.IAM.Menus.Validators;
using FluentValidation;

namespace ST.Application.Features.Shared.Validation;

public sealed class CreateMenuCommandValidator : AbstractValidator<CreateMenuCommand>
{
    public CreateMenuCommandValidator()
    {
        RuleFor(x => x.Request).SetValidator(new CreateMenuRequestValidator());
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Current user is required.");
    }
}
