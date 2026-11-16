using FluentValidation;
using ST.SharedKernel.Features.ControlPlane.Kyc;
using ST.Application.Features.ControlPlane.Kyc.Commands;

namespace ST.Application.Features.ControlPlane.Kyc.Validators;

public sealed class UploadKycDocumentCommandValidator : AbstractValidator<UploadKycDocumentCommand>
{
    public UploadKycDocumentCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.DocumentType).NotEmpty().MaximumLength(80);
        RuleFor(command => command.OtherDocumentName).MaximumLength(80);
        RuleFor(command => command.OtherDocumentName).NotEmpty()
            .When(command => string.Equals(command.DocumentType, KycDocumentTypes.Other, StringComparison.OrdinalIgnoreCase));

        RuleFor(command => command.OtherDocumentName).Empty()
            .When(command => !string.Equals(command.DocumentType, KycDocumentTypes.Other, StringComparison.OrdinalIgnoreCase));

        RuleFor(command => command.FileName).NotEmpty().MaximumLength(256);
        RuleFor(command => command.Length).InclusiveBetween(1, 10_485_760);
        RuleFor(command => command.Content).NotNull();
    }
}
