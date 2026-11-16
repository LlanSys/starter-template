namespace ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

public sealed record InspectKycSubmissionRequest(string Token)
{
    public override string ToString()
    {
        return nameof(InspectKycSubmissionRequest);
    }
}
