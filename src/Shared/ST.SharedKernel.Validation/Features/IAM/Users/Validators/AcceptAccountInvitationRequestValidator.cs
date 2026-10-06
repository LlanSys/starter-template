using ST.SharedKernel.Features.IAM.Users.Dtos;
using FluentValidation;

namespace ST.SharedKernel.Validation.Features.IAM.Users.Validators;

public sealed class AcceptAccountInvitationRequestValidator : AbstractValidator<AcceptAccountInvitationRequest>
{
    public AcceptAccountInvitationRequestValidator()
    {
        RuleFor(request => request.UserId).NotEmpty().MaximumLength(450);
        RuleFor(request => request.Token).NotEmpty().MaximumLength(8192);
        RuleFor(request => request.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(request => request.ConfirmPassword).Equal(request => request.Password);
    }
}
