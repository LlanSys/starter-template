using ST.Application.Features.Banking.Customers.CommandHandlers;
using ST.SharedKernel.Validation.Features.Banking.Customers.Validators;
using FluentValidation;

namespace ST.Application.Features.Shared.Validation;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.CreateCustomerRequest).SetValidator(new CreateCustomerRequestValidator());
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Current user is required.");
    }
}
