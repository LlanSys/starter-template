using FluentValidation;
using ST.Application.Features.ControlPlane.Staff.Commands;
using ST.SharedKernel.Features.ControlPlane;

namespace ST.Application.Features.ControlPlane.Staff.Validators;

public sealed class CreatePlatformStaffCommandValidator : AbstractValidator<CreatePlatformStaffCommand>
{
    public CreatePlatformStaffCommandValidator()
    {
        RuleFor(x => x.Request).NotNull();
        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.Username).NotEmpty().MaximumLength(50).Matches("^[A-Za-z0-9._@+-]+$");
            RuleFor(x => x.Request.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.Request.FirstName).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Request.LastName).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Request.Reason).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Request.PermissionKeys).NotEmpty().Must(keys => keys is not null && keys.Length <= 50 && keys.All(PlatformPermissions.IsDefined));
        });
    }
}
