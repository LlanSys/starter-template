using ST.Application.Features.Banking.Customers.CommandHandlers;
using ST.SharedKernel.Validation.Features.Banking.Customers.Validators;
using FluentValidation;

namespace ST.Application.Features.Shared.Validation;

public sealed class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Customer ID is required.");
        RuleFor(x => x.UpdateCustomerRequest).SetValidator(new UpdateCustomerRequestValidator());
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Current user is required.");
    }
}
