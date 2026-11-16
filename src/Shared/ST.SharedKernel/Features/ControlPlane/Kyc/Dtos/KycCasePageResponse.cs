namespace ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

public sealed record KycCasePageResponse(IReadOnlyList<KycCaseSummaryResponse> Items, int TotalRecords, int Page, int PageSize);
