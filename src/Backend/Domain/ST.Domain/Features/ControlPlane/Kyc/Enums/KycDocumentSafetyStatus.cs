namespace ST.Domain.Features.ControlPlane.Kyc.Enums;

public enum KycDocumentSafetyStatus
{
    Pending = 0,
    Scanning = 1,
    Clean = 2,
    Unsafe = 3,
    Blocked = 4,
    Failed = 5,
    Erased = 6
}
