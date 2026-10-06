using ST.Application.Features.IAM.Users.Commands;
using FluentValidation;

namespace ST.Application.Features.IAM.Users.Validators;

public sealed class UpdateUserRolesCommandValidator : AbstractValidator<UpdateUserRolesCommand>
{
    public UpdateUserRolesCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty().MaximumLength(450);
        RuleFor(command => command.UpdatedBy).NotEmpty().MaximumLength(450);
        RuleFor(command => command.Request).NotNull();
        RuleFor(command => command.Request.Roles).NotNull();
        RuleForEach(command => command.Request.Roles).NotEmpty().MaximumLength(80);
    }
}
