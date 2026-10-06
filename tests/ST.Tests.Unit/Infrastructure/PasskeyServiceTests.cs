using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Services;
using Fido2NetLib;
using Fido2NetLib.Objects;
using System.Text;
using System.Text.Json;

namespace ST.Tests.Unit.Infrastructure;

public sealed class PasskeyServiceTests
{
    private static PasskeyService CreateService() => new(new Fido2(new Fido2Configuration
    {
        ServerDomain = "localhost",
        ServerName = "Test",
        Origins = new HashSet<string> { "https://localhost" }
    }));

    [Fact]
    public async Task RegistrationOptions_PreserveUserAndExcludeExistingCredential()
    {
        var user = new AppUser { Id = "test-user", UserName = "test@example.test", FirstName = "Test", LastName = "User" };
        var credentialId = new byte[] { 1, 2, 3 };
        var json = await CreateService().RequestNewCredentialAsync(user,
            [new Fido2Credential { CredentialId = credentialId, CreatedBy = user.Id }]);
        var options = json.Deserialize<CredentialCreateOptions>();
        Assert.NotNull(options);
        Assert.Equal(Encoding.UTF8.GetBytes(user.Id), options.User.Id);
        Assert.Equal(credentialId, Assert.Single(options.ExcludeCredentials).Id);
        Assert.NotEmpty(options.Challenge);
    }

    [Fact]
    public async Task AssertionOptions_AllowDiscoverableCredentialsAndRoundTripChallenge()
    {
        var json = await CreateService().RequestAssertionAsync(string.Empty);
        var options = json.Deserialize<AssertionOptions>();
        Assert.NotNull(options);
        Assert.Empty(options.AllowCredentials);
        Assert.NotEmpty(options.Challenge);
        Assert.Equal(UserVerificationRequirement.Preferred, options.UserVerification);
    }
}
