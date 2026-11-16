namespace ST.Application.Features.ControlPlane.Kyc.Contracts;

public sealed record KycStoredObject(string Key, DateTimeOffset LastModified);
