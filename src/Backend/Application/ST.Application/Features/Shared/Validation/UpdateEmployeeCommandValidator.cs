using ST.Application.Features.HR.Employees.CommandHandlers;
using ST.SharedKernel.Validation.Features.HR.Employees.Validators;
using FluentValidation;

namespace ST.Application.Features.Shared.Validation;

public sealed class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Employee ID is required.");
        RuleFor(x => x.Request).SetValidator(new UpdateEmployeeRequestValidator());
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Current user is required.");
    }
}
