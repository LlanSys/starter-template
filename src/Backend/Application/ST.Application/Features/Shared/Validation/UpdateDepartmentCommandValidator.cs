using ST.Application.Features.HR.Departments.CommandHandlers;
using ST.SharedKernel.Validation.Features.HR.Departments.Validators;
using FluentValidation;

namespace ST.Application.Features.Shared.Validation;

public sealed class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Department ID is required.");
        RuleFor(x => x.Request).SetValidator(new UpdateDepartmentRequestValidator());
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Current user is required.");
    }
}
