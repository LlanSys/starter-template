using ST.Application.Features.IAM.Permissions.Commands;
using ST.SharedKernel.Validation.Features.IAM.Permissions.Validators;
using FluentValidation;

namespace ST.Application.Features.Shared.Validation;

public sealed class CreatePermissionCommandValidator : AbstractValidator<CreatePermissionCommand>
{
    public CreatePermissionCommandValidator()
    {
        RuleFor(x => x.Request).SetValidator(new CreatePermissionRequestValidator());
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Current user is required.");
    }
}
