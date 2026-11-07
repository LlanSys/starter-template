using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Dtos.Utilities;
using ST.SharedKernel.Features.ControlPlane;
using ST.SharedKernel.Features.ControlPlane.Stamps.Dtos;
using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration.ControlPlane;

[Collection("Starter API")]
public sealed class DeploymentStampGraphQLTests(StarterTemplateWebApplicationFactory factory)
{
    private const string GraphQLPath = "/graphql";
    private const string RestPath = "/api/v1/control-plane/stamps";

    private const string CreateMutation = """
        mutation CreateStamp($input: CreateDeploymentStampInput!) {
          createDeploymentStamp(input: $input) {
            stamp { id name targetResourceGroup isolationTier }
            errors { code message field }
          }
        }
        """;

    private const string UpdateMutation = """
        mutation UpdateStamp($id: UUID!, $input: UpdateDeploymentStampInput!) {
          updateDeploymentStamp(id: $id, input: $input) {
            stamp { id name targetResourceGroup }
            errors { code message field }
          }
        }
        """;

    [Fact]
    public async Task AnonymousRequestsMustAuthenticate()
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(GraphQLPath, new { query = "{ deploymentStamps { id } }" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task GraphQLReadsAndWritesRequirePlatformStaffAndPermission(bool staff, bool permission)
    {
        using var client = CreateClient(staff, permission);
        var query = await SendAsync(client, "{ deploymentStamps { id } }");
        AssertAuthorizationError(query);

        var result = await SendAsync(client, CreateMutation, new
        {
            input = new { name = "denied-stamp", targetResourceGroup = "example", isolationTier = "Pooled" }
        });
        AssertAuthorizationError(result);
    }

    [Fact]
    public async Task GraphQLAndRestShareDataAndGraphQLUsesTheExistingValidationPipeline()
    {
        var actorId = await SeedPlatformActorAsync();
        using var client = CreateClient(staff: true, permission: true, actorId: actorId);
        var name = $"graphql-{Guid.CreateVersion7():N}";
        var createInput = new
        {
            name, targetResourceGroup = "graphql-create", isolationTier = "Pooled",
            databaseConnectionString = "Server=example.invalid;Database=GraphQLTest"
        };
        var created = Payload(await SendAsync(client, CreateMutation, new { input = createInput }), "createDeploymentStamp");
        Assert.Empty(created.GetProperty("errors").EnumerateArray());
        var id = created.GetProperty("stamp").GetProperty("id").GetGuid();

        var fromRest = await client.GetFromJsonAsync<AppResponse<DeploymentStampResponse>>($"{RestPath}/{id:D}");
        Assert.True(fromRest?.IsSuccess);
        Assert.Equal("graphql-create", fromRest.Data!.TargetResourceGroup);
        var initialList = await SendAsync(client, "{ deploymentStamps { id targetResourceGroup } }");
        var initialStamp = Assert.Single(initialList.GetProperty("data").GetProperty("deploymentStamps").EnumerateArray(),
            stamp => stamp.GetProperty("id").GetGuid() == id);
        Assert.Equal("graphql-create", initialStamp.GetProperty("targetResourceGroup").GetString());

        var invalid = Payload(await SendAsync(client, UpdateMutation, new
        {
            id,
            input = new { name = "", targetResourceGroup = "invalid-update", isolationTier = "Pooled" }
        }), "updateDeploymentStamp");
        Assert.Equal(JsonValueKind.Null, invalid.GetProperty("stamp").ValueKind);
        var validation = Assert.Single(invalid.GetProperty("errors").EnumerateArray());
        Assert.Equal(ErrorCodes.Validation, validation.GetProperty("code").GetString());
        Assert.Equal("input.name", validation.GetProperty("field").GetString());

        var duplicate = Payload(await SendAsync(client, CreateMutation, new { input = createInput }), "createDeploymentStamp");
        Assert.Equal(ErrorCodes.BusinessRule, Assert.Single(duplicate.GetProperty("errors").EnumerateArray()).GetProperty("code").GetString());

        var updated = Payload(await SendAsync(client, UpdateMutation, new
        {
            id,
            input = new { name, targetResourceGroup = "graphql-update", isolationTier = "Pooled" }
        }), "updateDeploymentStamp");
        Assert.Empty(updated.GetProperty("errors").EnumerateArray());

        using var restUpdate = await client.PutAsJsonAsync($"{RestPath}/{id:D}", new
        {
            name, targetResourceGroup = "rest-update", isolationTier = "Pooled"
        });
        Assert.Equal(HttpStatusCode.OK, restUpdate.StatusCode);

        var data = await SendAsync(client, """
            query StampAndList($id: UUID!) {
              deploymentStamp(id: $id) { name targetResourceGroup }
              deploymentStamps { id name targetResourceGroup }
            }
            """, new { id });
        Assert.False(data.TryGetProperty("errors", out _), data.GetRawText());
        Assert.Equal("rest-update", data.GetProperty("data").GetProperty("deploymentStamp").GetProperty("targetResourceGroup").GetString());
        var listed = Assert.Single(data.GetProperty("data").GetProperty("deploymentStamps").EnumerateArray(),
            stamp => stamp.GetProperty("id").GetGuid() == id);
        Assert.Equal("rest-update", listed.GetProperty("targetResourceGroup").GetString());

        var schema = await SendAsync(client, """
            { __type(name: "DeploymentStamp") { fields { name } } }
            """);
        var fields = schema.GetProperty("data").GetProperty("__type").GetProperty("fields").EnumerateArray();
        Assert.DoesNotContain(fields, field => field.GetProperty("name").GetString() == "databaseConnectionString");
    }

    private static void AssertAuthorizationError(JsonElement response)
    {
        var error = Assert.Single(response.GetProperty("errors").EnumerateArray());
        Assert.Equal("AUTH_NOT_AUTHORIZED", error.GetProperty("extensions").GetProperty("code").GetString());
    }

    private static JsonElement Payload(JsonElement response, string operation)
    {
        Assert.False(response.TryGetProperty("errors", out _), response.GetRawText());
        return response.GetProperty("data").GetProperty(operation);
    }

    private static async Task<JsonElement> SendAsync(HttpClient client, string query, object? variables = null)
    {
        using var response = await client.PostAsJsonAsync(GraphQLPath, new { query, variables });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private HttpClient CreateClient(bool staff, bool permission, string? actorId = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.AuthenticationScheme);
        client.DefaultRequestHeaders.Add("X-Test-TenantId", Tenant.PlatformTenantId.ToString());
        if (staff)
        {
            client.DefaultRequestHeaders.Add("X-Test-PlatformStaff", "true");
        }
        if (permission)
        {
            client.DefaultRequestHeaders.Add("X-Test-Permissions", PlatformAccess.Permission);
        }
        if (actorId is not null)
        {
            client.DefaultRequestHeaders.Add("X-Test-UserId", actorId);
        }
        return client;
    }

    private async Task<string> SeedPlatformActorAsync()
    {
        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = Tenant.PlatformTenantId;
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var address = $"graphql-{Guid.CreateVersion7():N}@example.test";
        var actor = AppUser.CreateAccount(DateTimeOffset.UnixEpoch, Tenant.PlatformTenantId,
            address, "GraphQL", "Operator", address, null, "integration");
        actor.IsActive = true;
        actor.EmailConfirmed = true;
        actor.TwoFactorEnabled = true;
        Assert.True((await manager.CreateAsync(actor)).Succeeded);
        return actor.Id;
    }
}
