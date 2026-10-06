using ST.Application.Features.HR.Employees.CommandHandlers;
using ST.SharedKernel.Validation.Features.HR.Employees.Validators;
using FluentValidation;

namespace ST.Application.Features.Shared.Validation;

public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.Request).SetValidator(new CreateEmployeeRequestValidator());
        RuleFor(x => x.User).NotEmpty().WithMessage("Current user is required.");
    }
}
