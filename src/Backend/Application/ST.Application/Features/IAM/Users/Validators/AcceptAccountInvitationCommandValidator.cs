using ST.Application.Features.IAM.Users.Commands;
using ST.SharedKernel.Validation.Features.IAM.Users.Validators;
using FluentValidation;

namespace ST.Application.Features.IAM.Users.Validators;

public sealed class AcceptAccountInvitationCommandValidator : AbstractValidator<AcceptAccountInvitationCommand>
{
    public AcceptAccountInvitationCommandValidator() => RuleFor(command => command.Request).NotNull().SetValidator(new AcceptAccountInvitationRequestValidator());
}
