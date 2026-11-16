namespace ST.Domain.Features.ControlPlane.Kyc.Entities;

public sealed class KycRetentionPolicySelection
{
    public Guid Id { get; set; } = KycRetentionPolicy.DefaultId;
    public Guid PolicyId { get; set; } = KycRetentionPolicy.DefaultId;
    public Guid Revision { get; set; } = KycRetentionPolicy.DefaultId;
}
