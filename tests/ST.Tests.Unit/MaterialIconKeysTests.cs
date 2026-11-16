using ST.SharedKernel.Features.IAM.Menus;
using ST.Application.Features.IAM.ReferenceData.Commands;
using ST.Application.Features.Shared.Validation;
using ST.SharedKernel.Features.IAM.ReferenceData.Dtos;
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
    [Theory]
    [InlineData("Filled.Dashboard")]
    [InlineData("Outlined.AccountCircle")]
    [InlineData("Rounded.Settings")]
    [InlineData("Sharp.Menu")]
    [InlineData("TwoTone.Home")]
    [InlineData("Menu")]
    public void IconCustomizationAcceptsMaterialNamesWithoutRelaxingOtherCatalogues(string key)
    {
        var request = new ReferenceCatalogItemRequest { Key = key, Label = "Custom label", Description = "" };
        var validator = new CreateReferenceCatalogItemCommandValidator();
        Assert.True(validator.Validate(new CreateReferenceCatalogItemCommand("menu-icons", request, "test-actor")).IsValid);
        Assert.False(validator.Validate(new CreateReferenceCatalogItemCommand("permission-contexts", request, "test-actor")).IsValid);
    }

}
