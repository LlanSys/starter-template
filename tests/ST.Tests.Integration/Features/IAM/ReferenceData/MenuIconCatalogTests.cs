using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ST.Application.Features.IAM.ReferenceData.Commands;
using ST.Application.Features.IAM.ReferenceData.Queries;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.SharedKernel.Features.IAM.Menus;
using ST.SharedKernel.Features.IAM.ReferenceData.Dtos;
using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration.Features.IAM.ReferenceData;

[Collection("Starter API")]
public sealed class MenuIconCatalogTests(StarterTemplateWebApplicationFactory factory)
{
    [Fact]
    public async Task CatalogueContainsEveryStyleAndPreservesAnExistingShortKeyCustomization()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = Tenant.PlatformTenantId;
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var before = await sender.Send(new GetReferenceCatalogQuery("menu-icons"));
        Assert.True(before.IsSuccess);
        var displayed = before.Data!.Select(item => MaterialIconKeys.Normalize(item.Key)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.True(MaterialIconKeys.All.All(displayed.Contains));
        // Unconfigured icons are library definitions, not fabricated persisted records.
        Assert.Contains(before.Data!, item => item.Id == Guid.Empty && item.Key == "TwoTone.Home");

        var created = await sender.Send(new CreateReferenceCatalogItemCommand("menu-icons",
            new ReferenceCatalogItemRequest { Key = "Outlined.AccountCircle", Label = "Customer account", Description = "Identity navigation" }, "test-actor"));

        Assert.True(created.IsSuccess, created.Message);
        Assert.NotEqual(Guid.Empty, created.Data!.Id);
        var after = await sender.Send(new GetReferenceCatalogQuery("menu-icons"));
        var customized = Assert.Single(after.Data!, item => MaterialIconKeys.Normalize(item.Key).Equals("Outlined.AccountCircle", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(created.Data.Id, customized.Id);
        Assert.Equal("Customer account", customized.Label);
        Assert.Equal(before.Data!.Count, after.Data!.Count);
        Assert.Contains(after.Data, item => item.Key == "Menu" && item.Id != Guid.Empty);
        Assert.DoesNotContain(after.Data, item => item.Key == "Filled.Menu");
    }

    [Fact]
    public async Task CustomizationRejectsNamesOutsideTheInstalledLibrary()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = Tenant.PlatformTenantId;
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var result = await sender.Send(new CreateReferenceCatalogItemCommand("menu-icons",
            new ReferenceCatalogItemRequest { Key = "not_a_material_icon", Label = "Invalid", Description = "" }, "test-actor"));

        Assert.False(result.IsSuccess);
    }
}
