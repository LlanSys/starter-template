using System.Text.RegularExpressions;
using PhoneNumbers;

namespace ST.SharedKernel.Features.Shared.Phone;

public static partial class PhoneNumberFormatter
{
    public const string DefaultCountryCode = "+254";
    private static readonly PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();

    public static PhoneNumberParts Normalize(
        string? countryCode,
        string? nationalNumber,
        string? fallbackE164 = null)
    {
        var cleanCountryCode = NormalizeCountryCode(countryCode);
        var cleanNationalNumber = DigitsOnly(nationalNumber);
        var country = CountryCallingCodeCatalog.FindByDialCode(cleanCountryCode)
            ?? throw new ArgumentException($"Unsupported country calling code: {cleanCountryCode}.");

        if (string.IsNullOrWhiteSpace(cleanNationalNumber) && !string.IsNullOrWhiteSpace(fallbackE164))
        {
            return NormalizeFromE164(fallbackE164);
        }

        if (cleanCountryCode == DefaultCountryCode && cleanNationalNumber.StartsWith('0'))
        {
            cleanNationalNumber = cleanNationalNumber[1..];
        }

        return NormalizeWithLibPhoneNumber(country, cleanNationalNumber);
    }

    public static PhoneNumberParts NormalizeFromE164(string phoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);

        var cleaned = phoneNumber.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
        cleaned = cleaned.Replace("-", string.Empty, StringComparison.Ordinal);
        cleaned = cleaned.Replace("(", string.Empty, StringComparison.Ordinal);
        cleaned = cleaned.Replace(")", string.Empty, StringComparison.Ordinal);

        if (cleaned.StartsWith('0'))
        {
            cleaned = $"{DefaultCountryCode}{cleaned[1..]}";
        }

        if (!cleaned.StartsWith('+'))
        {
            cleaned = $"{DefaultCountryCode}{cleaned}";
        }

        if (!E164Regex().IsMatch(cleaned))
        {
            throw new ArgumentException("Phone number must be a valid E.164 number, for example +254712345678.");
        }

        PhoneNumber parsed;
        try
        {
            parsed = PhoneUtil.Parse(cleaned, null);
        }
        catch (NumberParseException ex)
        {
            throw new ArgumentException("Phone number could not be parsed as a valid international number.", ex);
        }

        if (!PhoneUtil.IsValidNumber(parsed))
        {
            throw new ArgumentException("Phone number is not valid for its country numbering plan.");
        }

        var regionCode = PhoneUtil.GetRegionCodeForNumber(parsed) ?? string.Empty;
        var country = CountryCallingCodeCatalog.FindByE164(cleaned)
            ?? CountryCallingCodeCatalog.FindByDialCode($"+{parsed.CountryCode}")
            ?? throw new ArgumentException("Phone number country calling code is not supported.");
        var e164 = PhoneUtil.Format(parsed, PhoneNumberFormat.E164);
        var nationalNumber = PhoneUtil.GetNationalSignificantNumber(parsed);
        var numberType = PhoneUtil.GetNumberType(parsed).ToString();

        return new PhoneNumberParts(country.DialCode, nationalNumber, e164, regionCode, numberType);
    }

    private static PhoneNumberParts NormalizeWithLibPhoneNumber(CountryCallingCode country, string nationalNumber)
    {
        if (string.IsNullOrWhiteSpace(nationalNumber))
        {
            throw new ArgumentException("Phone national number is required.");
        }

        PhoneNumber parsed;
        try
        {
            parsed = PhoneUtil.Parse(nationalNumber, country.Iso2);
        }
        catch (NumberParseException ex)
        {
            throw new ArgumentException($"{country.Name} phone number could not be parsed.", ex);
        }

        if (!PhoneUtil.IsPossibleNumberForType(parsed, PhoneNumberType.MOBILE)
            && !PhoneUtil.IsPossibleNumber(parsed))
        {
            throw new ArgumentException($"{country.Name} phone number is not possible.");
        }

        if (!PhoneUtil.IsValidNumberForRegion(parsed, country.Iso2))
        {
            throw new ArgumentException($"{country.Name} phone number is not valid for the selected country.");
        }

        var e164 = PhoneUtil.Format(parsed, PhoneNumberFormat.E164);
        var normalizedNationalNumber = PhoneUtil.GetNationalSignificantNumber(parsed);
        var numberType = PhoneUtil.GetNumberType(parsed).ToString();

        return new PhoneNumberParts(country.DialCode, normalizedNationalNumber, e164, country.Iso2, numberType);
    }

    private static string NormalizeCountryCode(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return DefaultCountryCode;
        }

        var digits = DigitsOnly(countryCode);
        if (string.IsNullOrWhiteSpace(digits))
        {
            return DefaultCountryCode;
        }

        return $"+{digits}";
    }

    private static string DigitsOnly(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(char.IsDigit).ToArray());

    [GeneratedRegex(@"^\+[1-9]\d{7,14}$", RegexOptions.CultureInvariant)]
    private static partial Regex E164Regex();
}
