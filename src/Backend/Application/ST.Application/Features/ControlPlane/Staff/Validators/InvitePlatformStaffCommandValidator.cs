using FluentValidation;
using ST.Application.Features.ControlPlane.Staff.Commands;

namespace ST.Application.Features.ControlPlane.Staff.Validators;

public sealed class InvitePlatformStaffCommandValidator : AbstractValidator<InvitePlatformStaffCommand>
{
    public InvitePlatformStaffCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.Revision).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Request.Reason).NotEmpty().MaximumLength(500);
        });
    }
}
