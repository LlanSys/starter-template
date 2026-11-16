using FluentValidation;
using ST.Application.Features.ControlPlane.Staff.Commands;
using ST.SharedKernel.Features.ControlPlane;

namespace ST.Application.Features.ControlPlane.Staff.Validators;

public sealed class UpdatePlatformStaffCommandValidator : AbstractValidator<UpdatePlatformStaffCommand>
{
    public UpdatePlatformStaffCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request).NotNull();
        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.Revision).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Request.Reason).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Request.PermissionKeys).NotNull().Must(keys => keys is not null && keys.Length <= 50 && keys.All(PlatformPermissions.IsDefined));
            When(x => x.Request.IsStaff, () => RuleFor(x => x.Request.PermissionKeys).NotEmpty());
        });
    }
}
