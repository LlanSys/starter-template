using ST.Application.Features.Shared.OrgSettings.CommandHandlers;
using FluentValidation;

namespace ST.Application.Features.Shared.OrgSettings.Validation;

public class CreateOrgSettingCommandValidator : AbstractValidator<CreateOrgSettingCommand>
{
    public CreateOrgSettingCommandValidator()
    {
        RuleFor(x => x.Request.Key)
            .NotEmpty().WithMessage("Key is required.")
            .MaximumLength(100).WithMessage("Key must not exceed 100 characters.");

        RuleFor(x => x.Request.Value)
            .NotEmpty().WithMessage("Value is required.");
    }
}
