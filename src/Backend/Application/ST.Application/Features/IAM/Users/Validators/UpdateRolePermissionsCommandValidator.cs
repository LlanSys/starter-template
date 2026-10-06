using ST.Application.Features.IAM.Permissions.Commands;
using FluentValidation;

namespace ST.Application.Features.IAM.Users.Validators;

public sealed class UpdateRolePermissionsCommandValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    public UpdateRolePermissionsCommandValidator()
    {
        RuleFor(command => command.RoleId).NotEmpty().MaximumLength(450);
        RuleFor(command => command.UserId).NotEmpty().MaximumLength(450);
        RuleFor(command => command.Request).NotNull();
        RuleFor(command => command.Request.PermissionKeys).NotNull();
        RuleForEach(command => command.Request.PermissionKeys).NotEmpty().MaximumLength(160);
    }
}
