using ST.Application.Features.HR.Employees.CommandHandlers;
using FluentValidation;

namespace ST.Application.Features.Shared.Validation;

public sealed class DeleteEmployeeCommandValidator : AbstractValidator<DeleteEmployeeCommand>
{
    public DeleteEmployeeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Employee ID is required.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Current user is required.");
    }
}
