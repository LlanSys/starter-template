using FluentValidation;

namespace ST.Application.Features.Shared.Payments.CommandHandlers.Mpesa;

public sealed class SimulateMpesaC2BPaymentCommandValidator : AbstractValidator<SimulateMpesaC2BPaymentCommand>
{
    public SimulateMpesaC2BPaymentCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.");

        RuleFor(x => x.BillRefNumber)
            .NotEmpty().WithMessage("Bill reference number is required.");
    }
}
