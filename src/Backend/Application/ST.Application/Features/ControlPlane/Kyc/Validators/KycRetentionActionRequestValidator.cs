using FluentValidation;
using ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;
namespace ST.Application.Features.ControlPlane.Kyc.Validators;

public sealed class KycRetentionActionRequestValidator : AbstractValidator<KycRetentionActionRequest>
{
    public KycRetentionActionRequestValidator()
    {
        RuleFor(x => x.ExpectedRevision).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Action).Must(x => x is "hold" or "release" or "restore" or "apply-current-policy" or "Ended" or "Declined" or "Withdrawn");
    }
}
