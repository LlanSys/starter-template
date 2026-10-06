using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Validation.Validators.Common;
using FluentValidation;

namespace ST.SharedKernel.Validation.Features.IAM.Users.Validators;

public sealed class ResetPasswordRequestValidator : Validator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(request => request.Token).NotEmpty().MaximumLength(4096);
        RuleFor(request => request.NewPassword ?? request.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a non-alphanumeric character.");
        RuleFor(request => request.ConfirmPassword)
            .Equal(request => request.NewPassword ?? request.Password)
            .WithMessage("Password confirmation must match.");
    }
}
