using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Validation.Validators.Common;
using FluentValidation;

namespace ST.SharedKernel.Validation.Features.IAM.Users.Validators;

public sealed class GrantEmployeeSystemAccessRequestValidator : Validator<GrantEmployeeSystemAccessRequest>
{
    public GrantEmployeeSystemAccessRequestValidator()
    {
        RuleFor(request => request.Roles).NotNull().Must(static roles => roles != null && roles.Count > 0)
            .WithMessage("At least one role is required.");
        RuleForEach(request => request.Roles).NotEmpty().MaximumLength(80);
    }
}
