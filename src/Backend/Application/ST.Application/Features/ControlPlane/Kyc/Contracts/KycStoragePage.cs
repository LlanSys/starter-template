namespace ST.Application.Features.ControlPlane.Kyc.Contracts;

public sealed record KycStoragePage(IReadOnlyList<KycStoredObject> Items, string? NextCursor);
