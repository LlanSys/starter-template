namespace ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

public sealed record KycCaseSummaryResponse(Guid Id, string DisplayName, DateTimeOffset? KycApprovedAt)
{
    public IReadOnlyList<KycDocumentTypeOption> DocumentTypes { get; init; } = [];
}
