using ST.SharedKernel.Features.IAM.Menus.Dtos;
using ST.SharedKernel.Features.IAM.ReferenceData.Dtos;
using ST.SharedKernel.Validation.Features.IAM.Menus.Validators;
using ST.SharedKernel.Validation.Features.IAM.ReferenceData.Validators;

namespace ST.Tests.Unit;

public sealed class ApplicationRouteValidationTests
{
    [Theory]
    [InlineData("/", true)]
    [InlineData("/dashboard", true)]
    [InlineData("/admin/users?sort=name#top", true)]
    [InlineData("/admin/users%20archive", true)]
    [InlineData("https://example.com/admin", false)]
    [InlineData("http://example.com/admin", false)]
    [InlineData("//example.com/admin", false)]
    [InlineData("/\\example.com/admin", false)]
    [InlineData("/admin\\users", false)]
    [InlineData("/admin\nusers", false)]
    [InlineData("/admin\rusers", false)]
    [InlineData("admin/users", false)]
    [InlineData("javascript:alert(1)", false)]
    public void MenuAndReferenceCatalogAcceptOnlyApplicationRoutes(string route, bool expected)
    {
        var menu = new CreateMenuRequest { Title = "Test", Url = route };
        var reference = new ReferenceCatalogItemRequest { Key = "test", Label = "Test", Url = route };

        Assert.Equal(expected, new CreateMenuRequestValidator().Validate(menu).IsValid);
        Assert.Equal(expected, new ReferenceCatalogItemRequestValidator().Validate(reference).IsValid);
    }
}
