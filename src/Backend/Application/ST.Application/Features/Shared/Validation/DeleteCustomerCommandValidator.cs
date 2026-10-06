using ST.Application.Features.Banking.Customers.CommandHandlers;
using FluentValidation;

namespace ST.Application.Features.Shared.Validation;

public sealed class DeleteCustomerCommandValidator : AbstractValidator<DeleteCustomerCommand>
{
    public DeleteCustomerCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Customer ID is required.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Current user is required.");
    }
}
