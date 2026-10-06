using ST.SharedKernel.Features.HR.Departments.Dtos;
using ST.SharedKernel.Validation.Validators.Common;
using FluentValidation;

namespace ST.SharedKernel.Validation.Features.HR.Departments.Validators;

public sealed class UpdateDepartmentRequestValidator : Validator<UpdateDepartmentRequest>
{
    public UpdateDepartmentRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Department ID is required.");

        RuleFor(x => new CreateDepartmentRequest
        {
            Code = x.Code,
            Name = x.Name,
            Description = x.Description
        }).SetValidator(new CreateDepartmentRequestValidator());
    }
}
