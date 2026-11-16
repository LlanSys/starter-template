using FluentValidation;
using ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;
namespace ST.Application.Features.ControlPlane.Kyc.Validators;

public sealed class PublishKycRetentionPolicyRequestValidator : AbstractValidator<PublishKycRetentionPolicyRequest>
{
    public PublishKycRetentionPolicyRequestValidator()
    {
        RuleFor(x => x.ExpectedPolicyId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ApprovedMonths).InclusiveBetween(1, 1200);
        RuleFor(x => x.ClosedDays).InclusiveBetween(1, 36500);
        RuleFor(x => x.InactiveDays).InclusiveBetween(1, 3650);
        RuleFor(x => x.RecoveryDays).InclusiveBetween(1, 365);
        RuleFor(x => x.OrphanDays).InclusiveBetween(1, 365);
        RuleFor(x => x.HoldReviewDays).InclusiveBetween(1, 365);
    }
}
