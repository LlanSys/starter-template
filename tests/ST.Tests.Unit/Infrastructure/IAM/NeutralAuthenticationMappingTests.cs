using System.Security.Claims;
using System.Text.Json;
using ST.Application.Features.IAM.Users.Contracts.Dtos;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Domain.Features.IAM.Users.Entities;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Features.IAM.Users.Mappings;

namespace ST.Tests.Unit.Infrastructure.IAM;

public sealed class NeutralAuthenticationMappingTests
{
    [Fact]
    public void AccountMapperAndProjectionKeepDistinctIdentityFieldsAndPlatformFlags()
    {
        var user = Account();
        var mapped = user.ToAppUserResponse(["Operator"]);
        var projected = AppUserMapping.AsResponse.Compile()(user);

        foreach (var response in new[] { mapped, projected })
        {
            Assert.Equal("username", response.Username);
            Assert.Equal("first", response.FirstName);
            Assert.Equal("last", response.LastName);
            Assert.Equal("first last", response.FullName);
            Assert.Equal("mail@example.test", response.Email);
            Assert.Equal("+254700000000", response.PhoneNumber);
            Assert.Equal(user.TenantId, response.TenantId);
            Assert.Equal(user.Id, response.Id);
            Assert.False(response.IsActive);
            Assert.False(response.EmailConfirmed);
            Assert.True(response.TwoFactorEnabled);
            Assert.True(response.RequirePasswordChange);
            Assert.Equal(user.LastLoginAt, response.LastLoginAt);
            Assert.Equal("/api/v1/iam/users/me/profile-picture/content", response.ProfilePictureUrl!.OriginalString);
        }
        Assert.Equal("Operator", Assert.Single(mapped.Roles));
        Assert.Empty(projected.Roles);
    }

    [Fact]
    public void LoginFallbackRetainsPermissionsRolesTenantSessionAndEnrollmentState()
    {
        var user = Account();
        var login = Login(user.ToAppUserResponse(["Operator"])) with
        {
            UserClaims = [new ClaimResponse("permission", "Accounts.Edit"),
                new ClaimResponse("Permission", "accounts.edit"),
                new ClaimResponse("permission", "Accounts.View"),
                new ClaimResponse("permission", " "), new ClaimResponse("employee_id", "legacy")]
        };

        var current = login.ToCurrentUserResponse();

        Assert.Equal(user.Id, current.AppUserId);
        Assert.Equal(user.TenantId, current.TenantId);
        Assert.Equal("mail@example.test", current.Email);
        Assert.Equal("first", current.FirstName);
        Assert.Equal("last", current.LastName);
        Assert.Equal(login.SessionId, current.SessionId);
        Assert.True(current.IsAuthenticated);
        Assert.False(current.EmailConfirmed);
        Assert.True(current.TwoFactorEnabled);
        Assert.True(current.MfaEnrollmentRequired);
        Assert.Equal(["Accounts.Edit", "Accounts.View"], current.Permissions);
        Assert.Equal("Operator", Assert.Single(current.Roles));
        Assert.Equal(user.ToAppUserResponse().ProfilePictureUrl, current.ProfilePictureUrl);
    }

    [Fact]
    public void LoginFallbackDoesNotInventAuthenticationOrPermissions()
    {
        var login = Login(Account().ToAppUserResponse()) with { IsAuthenticated = false, UserClaims = null };
        var current = login.ToCurrentUserResponse();
        Assert.False(current.IsAuthenticated);
        Assert.Empty(current.Permissions!);
        Assert.Empty(current.Roles);
    }

    [Fact]
    public void LoginFallbackRejectsMissingAccountInformation()
        => Assert.Throws<ArgumentException>(() => (Login(Account().ToAppUserResponse()) with { UserInfo = null }).ToCurrentUserResponse());

    [Fact]
    public void AccountWireContractsContainNoBusinessIdentityFields()
    {
        var account = Account().ToAppUserResponse();
        var current = Login(account).ToCurrentUserResponse();
        foreach (var json in new[] { JsonSerializer.Serialize(account), JsonSerializer.Serialize(current) })
        {
            using var document = JsonDocument.Parse(json);
            var names = document.RootElement.EnumerateObject().Select(property => property.Name).ToArray();
            Assert.Contains("TenantId", names);
            Assert.DoesNotContain(names, name => name is "EmployeeId" or "CustomerId" or "StaffId" or "IdNumber" or "Gender");
            Assert.DoesNotContain("private-storage", json, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("bad-tenant")]
    [InlineData("")]
    public void IdentityContextRejectsMalformedTenantAndIgnoresLegacyContextClaims(string tenantClaim)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "account"), new Claim("tenant_id", tenantClaim),
            new Claim("employee_id", Guid.CreateVersion7().ToString()),
            new Claim("customer_id", Guid.CreateVersion7().ToString()), new Claim("active_context", "Employee")]));
        var context = UserIdentityContext.FromClaims(principal);
        Assert.Equal("account", context.AppUserId);
        Assert.Equal(Guid.Empty, context.TenantId);
    }

    [Fact]
    public void IdentityContextPreservesValidTenantAndAccountIdentifier()
    {
        var tenantId = Guid.CreateVersion7();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "account"), new Claim("tenant_id", tenantId.ToString())]));
        var context = UserIdentityContext.FromClaims(principal);
        Assert.Equal("account", context.AppUserId);
        Assert.Equal(tenantId, context.TenantId);
    }

    private static AppUser Account()
    {
        var user = AppUser.CreateAccount(Guid.CreateVersion7(), "username", "first", "last", "mail@example.test", "+254700000000", "actor");
        user.EmployeeId = Guid.CreateVersion7();
        user.CustomerId = Guid.CreateVersion7();
        user.TwoFactorEnabled = true;
        user.RequirePasswordChange = true;
        user.LastLoginAt = DateTimeOffset.UtcNow;
        user.SetProfilePicture(new Uri("https://private-storage.example.test/private-image"), "actor");
        return user;
    }

    private static LoginResponse Login(AppUserResponse user)
        => new(user.Id, user.FirstName, user.LastName, user.Email, false, false, true,
            "test-access", "test-refresh", Guid.CreateVersion7().ToString(), DateTimeOffset.UtcNow.AddMinutes(5),
            user, [], MfaEnrollmentRequired: true);
}
