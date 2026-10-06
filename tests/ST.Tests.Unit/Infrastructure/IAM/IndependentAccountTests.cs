using System.Security.Claims;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Services;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;
using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Features.IAM.Menus.Contracts.Repositories;
using ST.Domain.Features.IAM.Permissions.Contracts.Repositories;
using ST.Domain.Features.IAM.Users.Contracts.Repositories;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Features.IAM.Users.Events;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;
using ST.Persistence.Features.IAM;
using ST.Persistence.Features.IAM.DataContext;
using ST.Persistence.Features.IAM.Users.Repositories;
using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Validation.Features.IAM.Users.Validators;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace ST.Tests.Unit.Infrastructure.IAM;

public sealed class IndependentAccountTests
{
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private static readonly string ActorId = Guid.CreateVersion7().ToString();
    private const string Password = "AccountTest!234";

    [Fact]
    public async Task CreatePersistsIndependentTenantAccountProfileRolesAndNeutralEvent()
    {
        using var fixture = new Fixture();
        await fixture.AddRoleAsync("Operator");
        var response = await fixture.Create.Handle(Command(["Operator", "operator"]), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal(TenantId, response.Data.TenantId);
        Assert.Single(response.Data.Roles);
        fixture.Context.ChangeTracker.Clear();
        var user = await fixture.Context.Users.IgnoreQueryFilters().SingleAsync();


        Assert.Equal(ActorId, user.CreatedBy);
        Assert.Equal(TenantId, user.TenantId);
        var profile = await fixture.Context.AppUserProfiles.SingleAsync();
        Assert.Equal(user.Id, profile.AppUserId);
        Assert.Equal(ActorId, profile.CreatedBy);
        var createdEvent = fixture.Publisher.ReceivedCalls().SelectMany(call => call.GetArguments()).OfType<AppUserCreatedEvent>().Single();
        Assert.Equal(TenantId, createdEvent.TenantId);
        Assert.Equal(user.Id, createdEvent.UserId);
    }

    [Fact]
    public async Task DuplicateIdentityInAnotherTenantOrSoftDeletedAccountIsRejected()
    {
        using var fixture = new Fixture();
        var user = AppUser.CreateAccount(Guid.CreateVersion7(), "EXISTING", "Existing", "Account", "USER@EXAMPLE.TEST", null, ActorId);
        Assert.True((await fixture.Users.CreateAsync(user, Password)).Succeeded);
        user.IsDeleted = true;
        await fixture.Context.SaveChangesAsync();

        var response = await fixture.Create.Handle(Command(), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Contains("already exists", response.Message);
        Assert.Equal(1, await fixture.Context.Users.IgnoreQueryFilters().CountAsync());
        Assert.Empty(await fixture.Context.AppUserProfiles.ToListAsync());
    }

    [Fact]
    public async Task MissingTenantDoesNotCreateAnAccount()
    {
        using var fixture = new Fixture();
        fixture.Tenant.TenantId.Returns(Guid.Empty);

        var response = await fixture.Create.Handle(Command(), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Empty(await fixture.Context.Users.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task RoleRejectionRollsBackIdentityWithoutReturningProviderErrors()
    {
        using var fixture = new Fixture();
        await fixture.AddRoleAsync("Operator");
        fixture.Users.RejectRoleChanges = true;

        var response = await fixture.Create.Handle(Command(["Operator"]), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.DoesNotContain("InternalProviderDetail", response.Message);
        fixture.Context.ChangeTracker.Clear();
        Assert.Empty(await fixture.Context.Users.IgnoreQueryFilters().ToListAsync());
        Assert.Empty(await fixture.Context.AppUserProfiles.ToListAsync());
        Assert.Empty(fixture.Publisher.ReceivedCalls());
    }

    [Fact]
    public async Task ProfilePersistenceFailureRollsBackAccountAndMembership()
    {
        var profiles = Substitute.For<IAppUserProfileRepository>();
        profiles.CreateAsync(Arg.Any<AppUserProfile>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<AppUserProfile>(new InvalidOperationException("test persistence failure")));
        using var fixture = new Fixture(profiles);
        await fixture.AddRoleAsync("Operator");

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Create.Handle(Command(["Operator"]), CancellationToken.None));

        fixture.Context.ChangeTracker.Clear();
        Assert.Empty(await fixture.Context.Users.IgnoreQueryFilters().ToListAsync());
        Assert.Empty(await fixture.Context.Set<IdentityUserRole<string>>().ToListAsync());
        Assert.Empty(fixture.Publisher.ReceivedCalls());
    }

    [Fact]
    public async Task CreationEventFailureRollsBackAccountProfileAndMembership()
    {
        using var fixture = new Fixture();
        await fixture.AddRoleAsync("Operator");
        fixture.Publisher.Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new InvalidOperationException("test event failure")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Create.Handle(Command(["Operator"]), CancellationToken.None));

        fixture.Context.ChangeTracker.Clear();
        Assert.Empty(await fixture.Context.Users.IgnoreQueryFilters().ToListAsync());
        Assert.Empty(await fixture.Context.AppUserProfiles.ToListAsync());
        Assert.Empty(await fixture.Context.Set<IdentityUserRole<string>>().ToListAsync());
    }
    [Fact]
    public async Task UnknownRoleIsRejectedBeforeCreatingIdentity()
    {
        using var fixture = new Fixture();

        var response = await fixture.Create.Handle(Command(["Unknown"]), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Empty(await fixture.Context.Users.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task EditChangesNeutralProfileAndPreservesLifecycleAndAuditCreator()
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUserAsync();
        user.IsActive = true;
        user.EmailConfirmed = true;
        await fixture.Context.SaveChangesAsync();

        var response = await fixture.Update.Handle(EditCommand(user.Id), CancellationToken.None);

        Assert.True(response.IsSuccess);
        fixture.Context.ChangeTracker.Clear();
        var persisted = await fixture.Context.Users.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("edited@example.test", persisted.Email);
        Assert.True(persisted.IsActive);
        Assert.True(persisted.EmailConfirmed);
        Assert.Equal(ActorId, persisted.CreatedBy);
        Assert.Equal(ActorId, persisted.UpdatedBy);
        var profile = await fixture.Context.AppUserProfiles.SingleAsync();
        Assert.Equal(persisted.Email, profile.Email);
    }

    [Fact]
    public async Task EditDoesNotAccessAnotherTenantsAccount()
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUserAsync();
        fixture.Tenant.TenantId.Returns(Guid.CreateVersion7());

        var response = await fixture.Update.Handle(EditCommand(user.Id), CancellationToken.None);

        Assert.False(response.IsSuccess);
        fixture.Context.ChangeTracker.Clear();
        Assert.Equal("user@example.test", (await fixture.Context.Users.IgnoreQueryFilters().SingleAsync()).Email);
    }

    [Fact]
    public async Task EditRoleFailureRollsBackProfileAndExistingMembership()
    {
        using var fixture = new Fixture();
        await fixture.AddRoleAsync("OldRole");
        await fixture.AddRoleAsync("NewRole");
        var user = await fixture.AddUserAsync();
        Assert.True((await fixture.Users.AddToRolesAsync(user, ["OldRole"])).Succeeded);
        fixture.Users.RejectRoleChanges = true;

        var response = await fixture.Update.Handle(EditCommand(user.Id, ["NewRole"]), CancellationToken.None);

        Assert.False(response.IsSuccess);
        fixture.Context.ChangeTracker.Clear();
        var persisted = await fixture.Context.Users.IgnoreQueryFilters().SingleAsync();
        Assert.Equal("user@example.test", persisted.Email);
        Assert.Equal("OldRole", Assert.Single(await fixture.Users.GetRolesAsync(persisted)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public void CreateInputRejectsMissingOrTooShortPassword(string password)
    {
        var validator = new CreateAppUserRequestValidator();
        Assert.False(validator.Validate(Command().Request with { Password = password }).IsValid);
    }

    [Fact]
    public void NeutralRequestIsValidWithoutBusinessOrKycFields()
    {
        Assert.True(new CreateAppUserRequestValidator().Validate(Command().Request).IsValid);
        Assert.True(new UpdateAdminUserRequestValidator().Validate(EditCommand("id").Request).IsValid);
    }

    [Fact]
    public async Task PlatformClaimsRetainTenantSessionRolesAndPermissionsWithoutBusinessLinks()
    {
        using var fixture = new Fixture();
        await fixture.AddRoleAsync("Operator");
        var user = await fixture.AddUserAsync();


        user.TwoFactorEnabled = true;
        Assert.True((await fixture.Users.UpdateAsync(user)).Succeeded);
        Assert.True((await fixture.Users.AddToRolesAsync(user, ["Operator"])).Succeeded);
        var role = await fixture.Roles.FindByNameAsync("Operator");
        Assert.True((await fixture.Roles.AddClaimAsync(role!, new Claim("permission", "Accounts.View"))).Succeeded);
        Assert.True((await fixture.Users.AddClaimAsync(user, new Claim("permission", "Accounts.View"))).Succeeded);
        Assert.True((await fixture.Users.AddClaimAsync(user, new Claim("employee_id", Guid.CreateVersion7().ToString()))).Succeeded);
        var sessionId = Guid.CreateVersion7();
        var service = new ClaimsService(fixture.Users, fixture.Roles, NullLogger<ClaimsService>.Instance);

        var claims = await service.GetUserClaimsAsync(user, sessionId);

        Assert.Contains(claims, claim => claim.Type == ClaimTypes.NameIdentifier && claim.Value == user.Id);
        Assert.Contains(claims, claim => claim.Type == "tenant_id" && claim.Value == TenantId.ToString());
        Assert.Contains(claims, claim => claim.Type == "session_id" && claim.Value == sessionId.ToString());
        Assert.Contains(claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Operator");
        Assert.Contains(claims, claim => claim.Type == "mfa_enrolled" && claim.Value == "true");
        Assert.Single(claims, claim => claim.Type == "permission" && claim.Value == "Accounts.View");
        Assert.DoesNotContain(claims, claim => claim.Type is "employee_id" or "customer_id" or "active_context");
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task CurrentUserPreservesPlatformStateAndMfaEnrollmentPolicy(bool enrolled, bool required)
    {
        using var fixture = new Fixture();
        await fixture.AddRoleAsync("System Administrator");
        var user = await fixture.AddUserAsync();
        user.TwoFactorEnabled = enrolled;
        user.LastLoginAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        user.EmailConfirmed = true;
        user.SetProfilePicture(new Uri("https://private-storage.example.test/private-image"), ActorId);
        Assert.True((await fixture.Users.UpdateAsync(user)).Succeeded);
        Assert.True((await fixture.Users.AddToRolesAsync(user, ["System Administrator"])).Succeeded);
        var sessionId = Guid.CreateVersion7().ToString();
        var http = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim("tenant_id", TenantId.ToString()),
                    new Claim("session_id", sessionId),
                    new Claim("permission", "Accounts.View")], "test"))
            }
        };
        var handler = new GetCurrentUser(http, fixture.Users, Options.Create(new MfaSettings()), NullLogger<GetCurrentUser>.Instance);

        var response = await handler.Handle(new GetCurrentUserCommand(), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(TenantId, response.Data!.TenantId);
        Assert.Equal(user.Id, response.Data.AppUserId);
        Assert.Equal(sessionId, response.Data.SessionId);
        Assert.Equal(user.LastLoginAt, response.Data.LastLoginAt);
        Assert.True(response.Data.EmailConfirmed);
        Assert.True(response.Data.IsAuthenticated);
        Assert.Equal(enrolled, response.Data.TwoFactorEnabled);
        Assert.Equal(required, response.Data.MfaEnrollmentRequired);
        Assert.Equal("Accounts.View", Assert.Single(response.Data.Permissions!));
        Assert.Equal("/api/v1/iam/users/me/profile-picture/content", response.Data.ProfilePictureUrl!.OriginalString);
    }

    [Fact]
    public async Task CurrentUserWithoutIdentityReturnsExpectedFailure()
    {
        using var fixture = new Fixture();
        var handler = new GetCurrentUser(new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            fixture.Users, Options.Create(new MfaSettings()), NullLogger<GetCurrentUser>.Instance);
        var response = await handler.Handle(new GetCurrentUserCommand(), CancellationToken.None);
        Assert.False(response.IsSuccess);
        Assert.Null(response.Data);
    }
    [Fact]
    public async Task ActivateStandaloneAccountPreservesCredentialsVerificationAndMemberships()
    {
        using var fixture = new Fixture();
        await fixture.AddRoleAsync("Operator");
        var user = await fixture.AddUserAsync();
        user.EmailConfirmed = true;
        Assert.True((await fixture.Users.UpdateAsync(user)).Succeeded);
        Assert.True((await fixture.Users.AddToRolesAsync(user, ["Operator"])).Succeeded);
        var hash = user.PasswordHash;
        var oldStamp = user.SecurityStamp;
        var response = await fixture.Activate.Handle(new ActivateAdminUserCommand(user.Id, ActorId), CancellationToken.None);
        Assert.True(response.IsSuccess);
        fixture.Context.ChangeTracker.Clear();
        var persisted = await fixture.Context.Users.SingleAsync();
        Assert.True(persisted.IsActive);
        Assert.Equal(ActorId, persisted.ActivatedBy);
        Assert.Equal(hash, persisted.PasswordHash);
        Assert.True(persisted.EmailConfirmed);
        Assert.True(persisted.RequirePasswordChange);
        Assert.NotEqual(oldStamp, persisted.SecurityStamp);
        Assert.Equal("Operator", Assert.Single(await fixture.Users.GetRolesAsync(persisted)));
        var accessEvent = fixture.Publisher.ReceivedCalls().SelectMany(call => call.GetArguments()).OfType<AppUserAccessGrantedEvent>().Single();
        Assert.Equal(TenantId, accessEvent.TenantId);
        Assert.Equal(ActorId, accessEvent.GrantedBy);
    }

    [Fact]
    public async Task ActivateDoesNotInventEmailVerificationOrResetPassword()
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUserAsync();
        var hash = user.PasswordHash;
        await SeedAccessAsync(fixture, user);
        Assert.True((await fixture.Activate.Handle(new ActivateAdminUserCommand(user.Id, ActorId), CancellationToken.None)).IsSuccess);
        fixture.Context.ChangeTracker.Clear();
        var persisted = await fixture.Context.Users.SingleAsync();
        Assert.False(persisted.EmailConfirmed);
        Assert.Equal(hash, persisted.PasswordHash);
        Assert.All(await fixture.Context.AppUserSessions.ToListAsync(), session => Assert.True(session.IsRevoked));
        Assert.All(await fixture.Context.RefreshTokens.ToListAsync(), token => Assert.NotNull(token.RevokedAt));
    }

    [Fact]
    public async Task RepeatedActivationPreservesLifecycleAndExistingSessions()
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUserAsync();
        user.IsActive = true;
        user.ActivatedBy = "previous-actor";
        user.ActivatedAt = DateTimeOffset.UtcNow.AddDays(-1);
        Assert.True((await fixture.Users.UpdateAsync(user)).Succeeded);
        var stamp = user.SecurityStamp;
        await SeedAccessAsync(fixture, user);
        var response = await fixture.Activate.Handle(new ActivateAdminUserCommand(user.Id, ActorId), CancellationToken.None);
        Assert.True(response.IsSuccess);
        fixture.Context.ChangeTracker.Clear();
        var persisted = await fixture.Context.Users.SingleAsync();
        Assert.Equal("previous-actor", persisted.ActivatedBy);
        Assert.Equal(stamp, persisted.SecurityStamp);
        Assert.All(await fixture.Context.AppUserSessions.ToListAsync(), session => Assert.True(session.IsActive));
        Assert.Empty(fixture.Publisher.ReceivedCalls());
    }

    [Theory]
    [InlineData("tenant")]
    [InlineData("deleted")]
    [InlineData("unresolved")]
    public async Task AccessChangesRejectUnresolvedDeletedAndOtherTenantAccounts(string scenario)
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUserAsync();
        if (scenario == "tenant") user.TenantId = Guid.CreateVersion7();
        if (scenario == "deleted") user.IsDeleted = true;
        Assert.True((await fixture.Users.UpdateAsync(user)).Succeeded);
        if (scenario == "unresolved") fixture.Tenant.TenantId.Returns(Guid.Empty);
        var activation = await fixture.Activate.Handle(new ActivateAdminUserCommand(user.Id, ActorId), CancellationToken.None);
        var deactivation = await fixture.Deactivate.Handle(new DeactivateAdminUserCommand(user.Id, new DeactivateUserRequest("reason"), ActorId), CancellationToken.None);
        Assert.False(activation.IsSuccess);
        Assert.False(deactivation.IsSuccess);
        Assert.Empty(fixture.Publisher.ReceivedCalls());
    }

    [Fact]
    public async Task DeactivationAtomicallyTerminatesSessionsTokensAndRotatesStampWithNeutralEvent()
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUserAsync();
        user.IsActive = true;
        Assert.True((await fixture.Users.UpdateAsync(user)).Succeeded);
        var stamp = user.SecurityStamp;
        await SeedAccessAsync(fixture, user);
        var response = await fixture.Deactivate.Handle(new DeactivateAdminUserCommand(user.Id, new DeactivateUserRequest("  Access removed  "), ActorId), CancellationToken.None);
        Assert.True(response.IsSuccess);
        fixture.Context.ChangeTracker.Clear();
        var persisted = await fixture.Context.Users.SingleAsync();
        Assert.False(persisted.IsActive);
        Assert.Equal(ActorId, persisted.DeactivatedBy);
        Assert.Equal("Access removed", persisted.DeactivationReason);
        Assert.NotEqual(stamp, persisted.SecurityStamp);
        var session = await fixture.Context.AppUserSessions.SingleAsync();
        Assert.True(session.IsRevoked);
        Assert.False(session.IsActive);
        Assert.Equal(ActorId, session.UpdatedBy);
        var token = await fixture.Context.RefreshTokens.SingleAsync();
        Assert.NotNull(token.RevokedAt);
        Assert.Equal(ActorId, token.UpdatedBy);
        Assert.Equal("Access removed", token.RevokedReason);
        var accessEvent = fixture.Publisher.ReceivedCalls().SelectMany(call => call.GetArguments()).OfType<AppUserAccessRevokedEvent>().Single();
        Assert.Equal(TenantId, accessEvent.TenantId);
        Assert.Equal(ActorId, accessEvent.RevokedBy);
    }

    [Fact]
    public async Task AlreadyInactiveAccountStillCleansStaleAccessWithoutRewritingDeactivationActor()
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUserAsync();
        user.DeactivatedBy = "previous-actor";
        user.DeactivationReason = "Previous reason";
        Assert.True((await fixture.Users.UpdateAsync(user)).Succeeded);
        await SeedAccessAsync(fixture, user);
        Assert.True((await fixture.Deactivate.Handle(new DeactivateAdminUserCommand(user.Id, new DeactivateUserRequest("Cleanup"), ActorId), CancellationToken.None)).IsSuccess);
        fixture.Context.ChangeTracker.Clear();
        Assert.Equal("previous-actor", (await fixture.Context.Users.SingleAsync()).DeactivatedBy);
        Assert.True((await fixture.Context.AppUserSessions.SingleAsync()).IsRevoked);
        Assert.NotNull((await fixture.Context.RefreshTokens.SingleAsync()).RevokedAt);
        Assert.Empty(fixture.Publisher.ReceivedCalls());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AccessIdentityFailureRollsBackLifecycleAndDoesNotExposeProviderDetails(bool activate)
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUserAsync();
        user.IsActive = !activate;
        Assert.True((await fixture.Users.UpdateAsync(user)).Succeeded);
        await SeedAccessAsync(fixture, user);
        fixture.Users.RejectStampChanges = true;
        var response = activate
            ? await fixture.Activate.Handle(new ActivateAdminUserCommand(user.Id, ActorId), CancellationToken.None)
            : await fixture.Deactivate.Handle(new DeactivateAdminUserCommand(user.Id, new DeactivateUserRequest("Reason"), ActorId), CancellationToken.None);
        Assert.False(response.IsSuccess);
        Assert.DoesNotContain("InternalProviderDetail", response.Message ?? string.Empty, StringComparison.Ordinal);
        fixture.Context.ChangeTracker.Clear();
        Assert.Equal(!activate, (await fixture.Context.Users.SingleAsync()).IsActive);
        Assert.True((await fixture.Context.AppUserSessions.SingleAsync()).IsActive);
        Assert.Null((await fixture.Context.RefreshTokens.SingleAsync()).RevokedAt);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AccessEventFailureRollsBackAccountStampSessionsAndTokens(bool activate)
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUserAsync();
        user.IsActive = !activate;
        Assert.True((await fixture.Users.UpdateAsync(user)).Succeeded);
        var stamp = user.SecurityStamp;
        await SeedAccessAsync(fixture, user);
        fixture.Publisher.Publish(Arg.Any<ST.Domain.Shared.Contracts.Common.IDomainEvent>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new InvalidOperationException("Event failure")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => activate
            ? fixture.Activate.Handle(new ActivateAdminUserCommand(user.Id, ActorId), CancellationToken.None)
            : fixture.Deactivate.Handle(new DeactivateAdminUserCommand(user.Id, new DeactivateUserRequest("Reason"), ActorId), CancellationToken.None));
        fixture.Context.ChangeTracker.Clear();
        var persisted = await fixture.Context.Users.SingleAsync();
        Assert.Equal(!activate, persisted.IsActive);
        Assert.Equal(stamp, persisted.SecurityStamp);
        Assert.True((await fixture.Context.AppUserSessions.SingleAsync()).IsActive);
        Assert.Null((await fixture.Context.RefreshTokens.SingleAsync()).RevokedAt);
    }

    private static async Task SeedAccessAsync(Fixture fixture, AppUser user)
    {
        fixture.Context.AppUserSessions.Add(AppUserSession.Create(Guid.CreateVersion7(), user.Id, "127.0.0.1", "test-agent", DateTimeOffset.UtcNow.AddHours(1), ActorId));
        fixture.Context.RefreshTokens.Add(ST.Domain.Features.IAM.Users.Entities.RefreshToken.Create(user.Id, Guid.CreateVersion7().ToString(), DateTimeOffset.UtcNow.AddDays(1), ActorId));
        await fixture.Context.SaveChangesAsync();
    }
    private static CreateAppUserCommand Command(ICollection<string>? roles = null) => new(
        new CreateAppUserRequest("user", "user@example.test", Password, "First", "Last", null, roles), ActorId);

    private static UpdateAdminUserCommand EditCommand(string userId, IReadOnlyList<string>? roles = null) => new(
        userId, new UpdateAdminUserRequest("edited", "edited@example.test", "Edited", "Person", null, roles ?? []), ActorId);

    [Fact]
    public async Task InvitationAcceptanceConfirmsEmailSetsPasswordAndRejectsReplay()
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUserAsync();
        user.IsActive = true;
        user.RequirePasswordChange = true;
        await fixture.Users.UpdateAsync(user);
        var token = await fixture.Users.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, AccountInvitation.Purpose(user));
        var request = new AcceptAccountInvitationRequest(user.Id, token, "ChosenPassword42!", "ChosenPassword42!");
        var response = await fixture.AcceptInvitation.Handle(new AcceptAccountInvitationCommand(request), CancellationToken.None);
        Assert.True(response.IsSuccess);
        fixture.Context.ChangeTracker.Clear();
        var persisted = (await fixture.Users.FindByIdAsync(user.Id))!;
        Assert.True(persisted.EmailConfirmed);
        Assert.False(persisted.RequirePasswordChange);
        Assert.Equal(user.Id, persisted.UpdatedBy);
        Assert.True(await fixture.Users.CheckPasswordAsync(persisted, "ChosenPassword42!"));
        Assert.False(await fixture.Users.CheckPasswordAsync(persisted, Password));
        Assert.False((await fixture.AcceptInvitation.Handle(new AcceptAccountInvitationCommand(request), CancellationToken.None)).IsSuccess);
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("deleted")]
    [InlineData("other-tenant")]
    [InlineData("changed-email")]
    [InlineData("tampered-token")]
    public async Task InvitationCannotBypassAccountOrTenantState(string rejection)
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUserAsync();
        user.IsActive = true;
        await fixture.Users.UpdateAsync(user);
        var token = await fixture.Users.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, AccountInvitation.Purpose(user));
        if (rejection == "disabled") user.IsActive = false;
        if (rejection == "deleted") user.IsDeleted = true;
        if (rejection == "other-tenant") fixture.Tenant.TenantId.Returns(Guid.CreateVersion7());
        if (rejection == "changed-email") user.Email = "changed@example.test";
        if (rejection == "tampered-token") token = "invalid-token";
        await fixture.Users.UpdateAsync(user);
        var response = await fixture.AcceptInvitation.Handle(new AcceptAccountInvitationCommand(
            new AcceptAccountInvitationRequest(user.Id, token, "ChosenPassword42!", "ChosenPassword42!")), CancellationToken.None);
        Assert.False(response.IsSuccess);
        Assert.False(user.EmailConfirmed);
        Assert.True(await fixture.Users.CheckPasswordAsync(user, Password));
    }

    [Fact]
    public async Task InvalidInvitationPasswordRollsBackConfirmationAndAudit()
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUserAsync();
        user.IsActive = true;
        await fixture.Users.UpdateAsync(user);
        var token = await fixture.Users.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, AccountInvitation.Purpose(user));
        var response = await fixture.AcceptInvitation.Handle(new AcceptAccountInvitationCommand(
            new AcceptAccountInvitationRequest(user.Id, token, "weak-password", "weak-password")), CancellationToken.None);
        Assert.False(response.IsSuccess);
        fixture.Context.ChangeTracker.Clear();
        var persisted = (await fixture.Users.FindByIdAsync(user.Id))!;
        Assert.False(persisted.EmailConfirmed);
        Assert.Null(persisted.PasswordLastChanged);
        Assert.True(await fixture.Users.CheckPasswordAsync(persisted, Password));
    }

    [Fact]
    public async Task InvitationDeliveryUsesProtectedLinkWithoutChangingCredentials()
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUserAsync();
        user.IsActive = true;
        user.FirstName = "<script>";
        await fixture.Users.UpdateAsync(user);
        var originalHash = user.PasswordHash;
        var originalStamp = user.SecurityStamp;
        ST.SharedKernel.Features.Shared.Notifications.Dtos.SendEmailRequest? sent = null;
        fixture.Email.SendEmailAsync(Arg.Any<ST.SharedKernel.Features.Shared.Notifications.Dtos.SendEmailRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => { sent = call.Arg<ST.SharedKernel.Features.Shared.Notifications.Dtos.SendEmailRequest>(); return ST.SharedKernel.Dtos.Common.AppResponses.Success<ST.SharedKernel.Features.Shared.Notifications.Dtos.SendEmailResponse>(null!); });
        var response = await fixture.Invite.Handle(new InviteAdminUserCommand(user.Id), CancellationToken.None);
        Assert.True(response.IsSuccess);
        Assert.NotNull(sent);
        Assert.Equal(user.Email, sent.To);
        Assert.Contains("https://starter.example.test/iam/accept-invitation?", sent.Body, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", sent.Body, StringComparison.Ordinal);
        Assert.DoesNotContain(Password, sent.Body, StringComparison.Ordinal);
        var linkMatch = System.Text.RegularExpressions.Regex.Match(sent.Body!, "href=\"([^\"]+)\"");
        var link = new Uri(System.Net.WebUtility.HtmlDecode(linkMatch.Groups[1].Value));
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(link.Query);
        Assert.Equal(user.Id, query["userId"].ToString());
        Assert.True(await fixture.Users.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, AccountInvitation.Purpose(user), query["token"].ToString()));
        Assert.Equal(originalHash, user.PasswordHash);
        Assert.Equal(originalStamp, user.SecurityStamp);
        Assert.False(user.EmailConfirmed);
    }

    [Fact]
    public async Task InvitationNoOpDeliveryCannotReportSuccessfulEmail()
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUserAsync();
        user.IsActive = true;
        await fixture.Users.UpdateAsync(user);
        fixture.EmailSettings.Provider = "NoOp";
        Assert.False((await fixture.Invite.Handle(new InviteAdminUserCommand(user.Id), CancellationToken.None)).IsSuccess);
        await fixture.Email.DidNotReceiveWithAnyArgs().SendEmailAsync(default!, default);
    }

    [Fact]
    public async Task PasskeyOwnershipAndGlobalUniquenessUsePersistedCredentials()
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUserAsync();
        user.IsActive = true;
        user.EmailConfirmed = true;
        await fixture.Users.UpdateAsync(user);
        byte[] credentialId = [12, 34, 56];
        byte[] handle = System.Text.Encoding.UTF8.GetBytes(user.Id);
        fixture.Context.Fido2Credentials.Add(new Fido2Credential
        {
            UserId = user.Id,
            TenantId = TenantId,
            CredentialId = credentialId,
            UserHandle = handle,
            PublicKey = [1],
            CreatedBy = ActorId
        });
        await fixture.Context.SaveChangesAsync();
        Assert.False(await fixture.PasskeyPolicy.IsUniqueAsync(user, credentialId, CancellationToken.None));
        Assert.True(await fixture.PasskeyPolicy.IsUniqueAsync(user, [99], CancellationToken.None));
        Assert.True(await fixture.PasskeyPolicy.IsOwnerAsync(user, credentialId, handle, CancellationToken.None));
        Assert.False(await fixture.PasskeyPolicy.IsOwnerAsync(user, credentialId, null, CancellationToken.None));
        Assert.False(await fixture.PasskeyPolicy.IsOwnerAsync(user, credentialId, [9], CancellationToken.None));
        fixture.Tenant.TenantId.Returns(Guid.CreateVersion7());
        Assert.False(await fixture.PasskeyPolicy.IsOwnerAsync(user, credentialId, handle, CancellationToken.None));
        fixture.Tenant.TenantId.Returns(TenantId);
        user.IsActive = false;
        Assert.False(await fixture.PasskeyPolicy.IsOwnerAsync(user, credentialId, handle, CancellationToken.None));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PasskeyChallengeIsTenantScopedExpiringAndConsumedOnce(bool expired)
    {
        using var fixture = new Fixture();
        var repo = new IamPasskeyChallengeRepository(fixture.Context);
        await repo.ReplaceAsync(new PasskeyChallenge
        {
            Key = "ceremony",
            TenantId = TenantId,
            OptionsJson = "protected-options",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(expired ? -1 : 5),
            CreatedBy = ActorId
        }, CancellationToken.None);
        await fixture.Context.SaveChangesAsync();
        fixture.Tenant.TenantId.Returns(Guid.CreateVersion7());
        Assert.Null(await repo.ConsumeAsync("ceremony", DateTime.UtcNow, CancellationToken.None));
        fixture.Tenant.TenantId.Returns(TenantId);
        Assert.Equal(expired ? null : "protected-options", await repo.ConsumeAsync("ceremony", DateTime.UtcNow, CancellationToken.None));
        Assert.Null(await repo.ConsumeAsync("ceremony", DateTime.UtcNow, CancellationToken.None));
    }

    [Fact]
    public async Task ConcurrentPasskeyConsumersHaveExactlyOneWinner()
    {
        string path = Path.Combine(Path.GetTempPath(), $"st-passkey-{Guid.CreateVersion7():N}.db");
        var tenant = Substitute.For<ICurrentTenantProvider>();
        tenant.TenantId.Returns(TenantId);
        var actor = Substitute.For<ICurrentActorProvider>();
        actor.ActorId.Returns(ActorId);
        var options = new DbContextOptionsBuilder<TestIamDBContext>().UseSqlite($"Data Source={path};Pooling=False").Options;
        try
        {
            await using (var setup = new TestIamDBContext(options, tenant, actor))
            {
                await setup.Database.EnsureCreatedAsync();
                setup.PasskeyChallenges.Add(new PasskeyChallenge
                {
                    Key = "concurrent",
                    TenantId = TenantId,
                    OptionsJson = "options",
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
                    CreatedBy = ActorId
                });
                await setup.SaveChangesAsync();
            }
            await using var first = new TestIamDBContext(options, tenant, actor);
            await using var second = new TestIamDBContext(options, tenant, actor);
            var results = await Task.WhenAll(new IamPasskeyChallengeRepository(first).ConsumeAsync("concurrent", DateTime.UtcNow, CancellationToken.None),
                new IamPasskeyChallengeRepository(second).ConsumeAsync("concurrent", DateTime.UtcNow, CancellationToken.None));
            Assert.Single(results, result => result == "options");
            Assert.Single(results, result => result is null);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PasswordRecoveryAtomicallyChangesCredentialsAndTerminatesAccess(bool failEvent)
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUserAsync();
        user.IsActive = true;
        user.EmailConfirmed = true;
        await fixture.Users.UpdateAsync(user);
        await SeedAccessAsync(fixture, user);
        var stamp = user.SecurityStamp;
        var token = await fixture.Users.GeneratePasswordResetTokenAsync(user);
        if (failEvent) fixture.Publisher.Publish(Arg.Any<PasswordResetSuccessEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("ProviderDetail")));
        const string replacement = "RecoveredPassword42!";
        var response = await fixture.Reset.Handle(new ResetPasswordCommand(new ResetPasswordRequest(
            user.Email!, replacement, null, replacement, token)), CancellationToken.None);
        Assert.Equal(!failEvent, response.IsSuccess);
        fixture.Context.ChangeTracker.Clear();
        var persisted = await fixture.Context.Users.SingleAsync();
        Assert.Equal(failEvent, await fixture.Users.CheckPasswordAsync(persisted, Password));
        Assert.Equal(!failEvent, await fixture.Users.CheckPasswordAsync(persisted, replacement));
        Assert.Equal(failEvent, (await fixture.Context.AppUserSessions.SingleAsync()).IsActive);
        Assert.Equal(failEvent, (await fixture.Context.RefreshTokens.SingleAsync()).RevokedAt is null);
        if (failEvent) Assert.Equal(stamp, persisted.SecurityStamp);
        else
        {
            Assert.NotEqual(stamp, persisted.SecurityStamp);
            Assert.False(persisted.RequirePasswordChange);
            Assert.Equal(user.Id, persisted.UpdatedBy);
            Assert.False(await fixture.Users.VerifyUserTokenAsync(persisted, TokenOptions.DefaultProvider,
                UserManager<AppUser>.ResetPasswordTokenPurpose, token));
        }
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("deleted")]
    [InlineData("unconfirmed")]
    public async Task EmailOtpCannotFinishLoginForAnUnavailableAccount(string state)
    {
        using var fixture = new Fixture();
        var user = await fixture.AddUserAsync();
        user.IsActive = state != "disabled";
        user.IsDeleted = state == "deleted";
        user.EmailConfirmed = state != "unconfirmed";
        await fixture.Users.UpdateAsync(user);
        var cache = Substitute.For<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
        var handler = new VerifyEmailOtp(fixture.Users, null!,
            Substitute.For<ST.Application.Features.IAM.Users.Contracts.Interfaces.IClaimsService>(),
            Substitute.For<ST.Application.Features.IAM.Users.Contracts.Interfaces.IJwtService>(), cache, fixture.UnitOfWork,
            Substitute.For<ST.Application.Features.IAM.Users.Contracts.Interfaces.ISessionService>(), new HttpContextAccessor(),
            Options.Create(new JwtSettings()), NullLogger<VerifyEmailOtp>.Instance);
        var response = await handler.Handle(new VerifyEmailOtpCommand(new VerifyEmailOtpRequest
        { UserId = user.Id, Code = "123456", Purpose = "Login" }), CancellationToken.None);
        Assert.False(response.IsSuccess);
        await cache.DidNotReceiveWithAnyArgs().GetAsync(default!, default);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"id\":\"!!\"}")]
    public async Task MalformedPasskeyAttemptConsumesChallengeAndReturnsSanitizedFailure(string json)
    {
        using var fixture = new Fixture();
        var correlation = Guid.CreateVersion7();
        var repo = fixture.UnitOfWork.PasskeyChallengeRepository;
        await repo.ReplaceAsync(new PasskeyChallenge
        {
            Key = PasskeyChallengeKeys.Assertion(correlation),
            TenantId = TenantId,
            OptionsJson = "{}",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            CreatedBy = ActorId
        }, CancellationToken.None);
        await fixture.Context.SaveChangesAsync();
        var passkeys = Substitute.For<ST.Application.Features.IAM.Users.Contracts.Interfaces.IPasskeyService>();
        var handler = new LoginWithPasskey(fixture.Users, new HttpContextAccessor(),
            Substitute.For<ST.Application.Features.IAM.Users.Contracts.Interfaces.IJwtService>(),
            Substitute.For<ST.Application.Features.IAM.Users.Contracts.Interfaces.IClaimsService>(),
            Substitute.For<ST.Application.Contracts.Interfaces.Common.IServiceManager>(), fixture.UnitOfWork,
            passkeys, TimeProvider.System, Options.Create(new JwtSettings()), NullLogger<LoginWithPasskey>.Instance);
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var response = await handler.Handle(new LoginWithPasskeyCommand(null, correlation, document.RootElement), CancellationToken.None);
        Assert.False(response.IsSuccess);
        Assert.DoesNotContain("Exception", response.Message!, StringComparison.Ordinal);
        Assert.Null(await repo.ConsumeAsync(PasskeyChallengeKeys.Assertion(correlation), DateTime.UtcNow, CancellationToken.None));
    }

    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        public TestIamDBContext Context { get; }
        public ICurrentTenantProvider Tenant { get; } = Substitute.For<ICurrentTenantProvider>();
        public IPublisher Publisher { get; } = Substitute.For<IPublisher>();
        public TestUserManager Users { get; }
        public IIamUnitOfWork UnitOfWork { get; }
        public RoleManager<AppRole> Roles { get; }
        public PasskeyCredentialPolicy PasskeyPolicy { get; }
        public CreateAppUser Create { get; }
        public UpdateAdminUser Update { get; }
        public ActivateAdminUser Activate { get; }
        public DeactivateAdminUser Deactivate { get; }
        public AcceptAccountInvitation AcceptInvitation { get; }
        public InviteAdminUser Invite { get; }
        public ResetPassword Reset { get; }
        public ST.Application.Features.Shared.Notifications.Contracts.Interfaces.IEmailService Email { get; } = Substitute.For<ST.Application.Features.Shared.Notifications.Contracts.Interfaces.IEmailService>();
        public ST.Infrastructure.Configuration.EmailSettings EmailSettings { get; } = new() { Provider = "LocalMailpit", ClientBaseUrl = "https://starter.example.test" };

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
            Justification = "UserManager and RoleManager own their stores and Fixture.Dispose disposes both managers.")]
        public Fixture(IAppUserProfileRepository? profiles = null)
        {
            Tenant.TenantId.Returns(TenantId);
            var actor = Substitute.For<ICurrentActorProvider>();
            actor.ActorId.Returns(ActorId);
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            Context = new TestIamDBContext(new DbContextOptionsBuilder<TestIamDBContext>().UseSqlite(_connection).Options, Tenant, actor);
            Context.Database.EnsureCreated();
            Users = new TestUserManager(new UserStore<AppUser, AppRole, TestIamDBContext>(Context));
            Users.RegisterTokenProvider(TokenOptions.DefaultProvider, new DataProtectorTokenProvider<AppUser>(
                new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider(),
                Microsoft.Extensions.Options.Options.Create(new DataProtectionTokenProviderOptions()),
                NullLogger<DataProtectorTokenProvider<AppUser>>.Instance));
            Roles = new RoleManager<AppRole>(new RoleStore<AppRole, TestIamDBContext>(Context),
                [new RoleValidator<AppRole>()], new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(), NullLogger<RoleManager<AppRole>>.Instance);
            var uow = new IamUnitOfWork(Context, Substitute.For<IUserRepository>(), new IamSessionRepository(Context),
                new IamTokenRepository(Context), profiles ?? new IamAppUserProfileRepository(Context),
                Substitute.For<IAppUserTotpSecretRepository>(), Substitute.For<ITempTotpSecretRepository>(),
                Substitute.For<IPermissionRepository>(), Substitute.For<IMenuRepository>(), Publisher, NullLogger<IamUnitOfWork>.Instance);
            UnitOfWork = uow;
            PasskeyPolicy = new PasskeyCredentialPolicy(uow, Tenant);
            Create = new CreateAppUser(Users, Roles, uow, Tenant, NullLogger<CreateAppUser>.Instance);
            Update = new UpdateAdminUser(Users, Roles, uow, Tenant, NullLogger<UpdateAdminUser>.Instance);
            Activate = new ActivateAdminUser(Users, uow, Tenant, NullLogger<ActivateAdminUser>.Instance);
            Deactivate = new DeactivateAdminUser(Users, uow, Tenant, NullLogger<DeactivateAdminUser>.Instance);
            Reset = new ResetPassword(Users, uow, Substitute.For<ST.Application.Contracts.Interfaces.Common.ICacheService>(), Publisher, Tenant, NullLogger<ResetPassword>.Instance);
            AcceptInvitation = new AcceptAccountInvitation(Users, uow, Tenant, NullLogger<AcceptAccountInvitation>.Instance);
            Invite = new InviteAdminUser(Users, Tenant, Email, Microsoft.Extensions.Options.Options.Create(EmailSettings), NullLogger<InviteAdminUser>.Instance);
        }

        public async Task AddRoleAsync(string name)
            => Assert.True((await Roles.CreateAsync(new AppRole { Id = Guid.CreateVersion7().ToString(), Name = name })).Succeeded);

        public async Task<AppUser> AddUserAsync()
        {
            var user = AppUser.CreateAccount(TenantId, "user", "First", "Last", "user@example.test", null, ActorId);
            Assert.True((await Users.CreateAsync(user, Password)).Succeeded);
            return user;
        }

        public void Dispose()
        {
            Users.Dispose();
            Roles.Dispose();
            Context.Dispose();
            _connection.Dispose();
        }
    }

    private sealed class TestIamDBContext(DbContextOptions<TestIamDBContext> options,
        ICurrentTenantProvider tenant, ICurrentActorProvider actor) : IamDBContext(options, tenant, actor)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // SQLite has no generated rowversion; retain the concurrency token with supplied test values.
            foreach (var entity in builder.Model.GetEntityTypes())
            {
                var rowVersion = entity.FindProperty("RowVersion");
                if (rowVersion is not null)
                {
                    rowVersion.ValueGenerated = ValueGenerated.Never;
                }
            }
        }
    }

    private sealed class TestUserManager(IUserStore<AppUser> store) : UserManager<AppUser>(store,
        Microsoft.Extensions.Options.Options.Create(new IdentityOptions()), new PasswordHasher<AppUser>(), [new UserValidator<AppUser>()],
        [new PasswordValidator<AppUser>()], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(),
        null!, NullLogger<UserManager<AppUser>>.Instance)
    {
        public bool RejectRoleChanges { get; set; }
        public bool RejectStampChanges { get; set; }

        public override Task<IdentityResult> UpdateSecurityStampAsync(AppUser user)
            => RejectStampChanges ? Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "InternalProviderDetail" }))
                : base.UpdateSecurityStampAsync(user);

        public override Task<IdentityResult> AddToRolesAsync(AppUser user, IEnumerable<string> roles)
            => RejectRoleChanges
                ? Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "InternalProviderDetail", Code = "TestRejected" }))
                : base.AddToRolesAsync(user, roles);
    }
}
