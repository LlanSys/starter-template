namespace ST.SharedKernel.Features.Shared.Phone;

public sealed record PhoneNumberParts(
    string CountryCode,
    string NationalNumber,
    string E164,
    string RegionCode = "",
    string NumberType = "");
