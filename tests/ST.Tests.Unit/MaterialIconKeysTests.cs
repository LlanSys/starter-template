using ST.SharedKernel.Features.IAM.Menus;
namespace ST.Tests.Unit;

public sealed class MaterialIconKeysTests
{
    [Theory]
    [InlineData("Dashboard")]
    [InlineData("Filled.Dashboard")]
    [InlineData("Outlined.AccountCircle")]
    [InlineData("Rounded.Settings")]
    [InlineData("Sharp.Menu")]
    [InlineData("TwoTone.Home")]
    [InlineData(" outlined.accountcircle ")]
    public void KnownKeysAreAccepted(string key) => Assert.True(MaterialIconKeys.Contains(key));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Filled.DoesNotExist")]
    [InlineData("<svg onload=alert(1)>")]
    public void UnknownKeysAndSvgAreRejected(string? key) => Assert.False(MaterialIconKeys.Contains(key));
}
