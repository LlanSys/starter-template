using FluentValidation;

namespace ST.Application.Features.Shared.Payments.CommandHandlers;

public sealed class ProcessPaymentWebhookCommandValidator : AbstractValidator<ProcessPaymentWebhookCommand>
{
    public ProcessPaymentWebhookCommandValidator()
    {
        RuleFor(command => command.Provider)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(command => command.Payload)
            .NotEmpty();

        RuleFor(command => command.SignatureHeader)
            .NotEmpty();
    }
}
