using ST.Application.Features.IAM.Users.Commands;
using FluentValidation;

namespace ST.Application.Features.IAM.Users.Validators;

public sealed class UpdateUserPermissionsCommandValidator : AbstractValidator<UpdateUserPermissionsCommand>
{
    public UpdateUserPermissionsCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty().MaximumLength(450);
        RuleFor(command => command.UpdatedBy).NotEmpty().MaximumLength(450);
        RuleFor(command => command.Request).NotNull();
        RuleFor(command => command.Request.PermissionKeys).NotNull();
        RuleForEach(command => command.Request.PermissionKeys).NotEmpty().MaximumLength(160);
    }
}
