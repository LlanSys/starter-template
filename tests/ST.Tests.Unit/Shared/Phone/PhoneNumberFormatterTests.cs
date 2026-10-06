using ST.SharedKernel.Features.Shared.Phone;

namespace ST.Tests.Unit.Shared.Phone;

public sealed class PhoneNumberFormatterTests
{
    [Theory]
    [InlineData("712345678")]
    [InlineData("0712345678")]
    [InlineData("+254712345678")]
    public void Normalize_KenyanMobileNumber_ReturnsNormalizedE164(string nationalNumber)
    {
        var phone = PhoneNumberFormatter.Normalize("+254", nationalNumber);

        Assert.Equal("+254", phone.CountryCode);
        Assert.Equal("712345678", phone.NationalNumber);
        Assert.Equal("+254712345678", phone.E164);
        Assert.Equal("KE", phone.RegionCode);
        Assert.Equal("MOBILE", phone.NumberType);
    }

    [Fact]
    public void NormalizeFromE164_UnitedStatesNumber_ReturnsRegionAndNormalizedNumber()
    {
        var phone = PhoneNumberFormatter.NormalizeFromE164("+14155552671");

        Assert.Equal("+1", phone.CountryCode);
        Assert.Equal("4155552671", phone.NationalNumber);
        Assert.Equal("+14155552671", phone.E164);
        Assert.Equal("US", phone.RegionCode);
    }

    [Fact]
    public void Normalize_InvalidKenyanNumber_ThrowsFriendlyValidationException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            PhoneNumberFormatter.Normalize("+254", "123"));

        Assert.Contains("Kenya phone number", exception.Message);
    }

    [Fact]
    public void Normalize_UnsupportedCountryCallingCode_ThrowsFriendlyValidationException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            PhoneNumberFormatter.Normalize("+999", "712345678"));

        Assert.Contains("Unsupported country calling code", exception.Message);
    }
}
