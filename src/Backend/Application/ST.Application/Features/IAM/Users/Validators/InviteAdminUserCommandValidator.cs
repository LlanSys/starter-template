using ST.Application.Features.IAM.Users.Commands;
using FluentValidation;

namespace ST.Application.Features.IAM.Users.Validators;

public sealed class InviteAdminUserCommandValidator : AbstractValidator<InviteAdminUserCommand>
{
    public InviteAdminUserCommandValidator() => RuleFor(command => command.UserId).NotEmpty().MaximumLength(450);
}
