using FluentValidation;
using ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

namespace ST.Application.Features.ControlPlane.Kyc.Validators;

public sealed class ReviewKycDocumentRequestValidator : AbstractValidator<ReviewKycDocumentRequest>
{
    public ReviewKycDocumentRequestValidator()
    {
        RuleFor(request => request.Feedback).NotEmpty().MaximumLength(2000);
    }
}
