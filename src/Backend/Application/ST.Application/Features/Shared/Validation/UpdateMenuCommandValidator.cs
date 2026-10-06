using ST.Application.Features.IAM.Menus.Commands;
using ST.SharedKernel.Validation.Features.IAM.Menus.Validators;
using FluentValidation;

namespace ST.Application.Features.Shared.Validation;

public sealed class UpdateMenuCommandValidator : AbstractValidator<UpdateMenuCommand>
{
    public UpdateMenuCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Menu ID is required.");
        RuleFor(x => x.Request).SetValidator(new UpdateMenuRequestValidator());
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Current user is required.");
    }
}
