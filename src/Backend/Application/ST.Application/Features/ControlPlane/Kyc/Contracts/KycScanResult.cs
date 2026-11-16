namespace ST.Application.Features.ControlPlane.Kyc.Contracts;

public sealed record KycScanResult(
    KycScanVerdict Verdict, string Engine, string Definitions, DateTimeOffset? DefinitionsAt,
    string ReasonCode);
