using FluentValidation;
using ST.Application.Features.ControlPlane.Kyc.Commands;

namespace ST.Application.Features.ControlPlane.Kyc.Validators;

public sealed class ReviewKycDocumentCommandValidator : AbstractValidator<ReviewKycDocumentCommand>
{
    public ReviewKycDocumentCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.DocumentId).NotEmpty();
        RuleFor(command => command.Request).NotNull().SetValidator(new ReviewKycDocumentRequestValidator());
    }
}
