using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Validation.Validators.Common;
using FluentValidation;

namespace ST.SharedKernel.Validation.Features.IAM.Users.Validators;

public sealed class RefreshTokenRequestValidator : Validator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(request => request.AccessToken).NotEmpty();
        RuleFor(request => request.RefreshToken).NotEmpty();
    }
}
