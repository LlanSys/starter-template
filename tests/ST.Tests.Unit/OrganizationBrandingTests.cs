using System.Globalization;
using ST.UI.Blazor.Features.IAM.Users.Utilities;
namespace ST.Tests.Unit;

public sealed class OrganizationBrandingTests
{
    [Theory]
    [InlineData("Alderfield QA Services", "AQS")]
    [InlineData("Brookmere QA Supplies", "BQS")]
    [InlineData("NorthwindTrading", "NT")]
    [InlineData("NorthwindSupplies", "NS")]
    [InlineData("Stripe", "S")]
    [InlineData("  alderfield   qa services  ", "AQS")]
    [InlineData("Alder & Brook", "AB")]
    [InlineData("Élan conseil", "ÉC")]
    [InlineData("東京 商事", "東商")]
    [InlineData("A B C D", "ABC")]
    [InlineData("", "?")]
    [InlineData(null, "?")]
    [InlineData("🙂", "?")]
    public void InitialsAreBoundedAndUseWholeTextElements(string? name, string expected)
    {
        Assert.Equal(expected, OrganizationBranding.Initials(name));
    }

    [Fact]
    public void CombiningMarksStayAttachedAndDisplayMetadataDoesNotRenameThePlatform()
    {
        var initials = OrganizationBranding.Initials("A\u0301rea Digital");
        Assert.Equal(2, new StringInfo(initials).LengthInTextElements);
        Assert.StartsWith("A\u0301", initials, StringComparison.Ordinal);
        Assert.Equal("Alderfield QA Services", OrganizationBranding.DisplayName("  Alderfield QA Services  "));
        Assert.Equal(OrganizationBranding.ProductName, OrganizationBranding.DisplayName("Default Tenant", true));
        Assert.Equal(OrganizationBranding.ProductName, OrganizationBranding.DisplayName(null));
    }
}
