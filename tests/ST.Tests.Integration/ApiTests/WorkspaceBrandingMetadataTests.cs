using System.Net;
using System.Net.Http.Json;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using ST.Tests.Integration.TestFixtures;
namespace ST.Tests.Integration.ApiTests;

[Collection("Starter API")]
public sealed class WorkspaceBrandingMetadataTests(StarterTemplateWebApplicationFactory factory)
{
    [Fact]
    public async Task ResolverReturnsTenantOwnedDisplayMetadataAndDistinguishesTheVendorWorkspace()
    {
        var id = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(id);
        using var anonymous = factory.CreateClient();
        var customer = await anonymous.GetFromJsonAsync<AppResponse<WorkspaceInfoResponse>>($"/api/v1/workspaces/resolve?tenantId={id}");
        Assert.True(customer!.IsSuccess);
        Assert.Equal(id, customer.Data!.TenantId);
        Assert.Equal("Test workspace", customer.Data.Name);
        Assert.False(customer.Data.IsPlatformWorkspace);
        var vendor = await anonymous.GetFromJsonAsync<AppResponse<WorkspaceInfoResponse>>($"/api/v1/workspaces/resolve?tenantId={Tenant.PlatformTenantId}");
        Assert.True(vendor!.IsSuccess);
        Assert.True(vendor.Data!.IsPlatformWorkspace);
        using var unavailable = await anonymous.GetAsync($"/api/v1/workspaces/resolve?tenantId={Guid.CreateVersion7()}");
        Assert.Equal(HttpStatusCode.NotFound, unavailable.StatusCode);
    }
}
