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
        Assert.Null(user.EmployeeId);
        Assert.Null(user.CustomerId);
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
        user.EmployeeId = Guid.CreateVersion7();
        user.CustomerId = Guid.CreateVersion7();
        user.TwoFactorEnabled = true;
        Assert.True((await fixture.Users.UpdateAsync(user)).Succeeded);
        Assert.True((await fixture.Users.AddToRolesAsync(user, ["Operator"])).Succeeded);
        var role = await fixture.Roles.FindByNameAsync("Operator");
        Assert.True((await fixture.Roles.AddClaimAsync(role!, new Claim("permission", "Accounts.View"))).Succeeded);
        Assert.True((await fixture.Users.AddClaimAsync(user, new Claim("permission", "Accounts.View"))).Succeeded);
        Assert.True((await fixture.Users.AddClaimAsync(user, new Claim("employee_id", user.EmployeeId.Value.ToString()))).Succeeded);
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

    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        public TestIamDBContext Context { get; }
        public ICurrentTenantProvider Tenant { get; } = Substitute.For<ICurrentTenantProvider>();
        public IPublisher Publisher { get; } = Substitute.For<IPublisher>();
        public TestUserManager Users { get; }
        public RoleManager<AppRole> Roles { get; }
        public CreateAppUser Create { get; }
        public UpdateAdminUser Update { get; }
        public ActivateAdminUser Activate { get; }
        public DeactivateAdminUser Deactivate { get; }

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
            Roles = new RoleManager<AppRole>(new RoleStore<AppRole, TestIamDBContext>(Context),
                [new RoleValidator<AppRole>()], new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(), NullLogger<RoleManager<AppRole>>.Instance);
            var uow = new IamUnitOfWork(Context, Substitute.For<IUserRepository>(), new IamSessionRepository(Context),
                new IamTokenRepository(Context), profiles ?? new IamAppUserProfileRepository(Context),
                Substitute.For<IAppUserTotpSecretRepository>(), Substitute.For<ITempTotpSecretRepository>(),
                Substitute.For<IPermissionRepository>(), Substitute.For<IMenuRepository>(), Publisher, NullLogger<IamUnitOfWork>.Instance);
            Create = new CreateAppUser(Users, Roles, uow, Tenant, NullLogger<CreateAppUser>.Instance);
            Update = new UpdateAdminUser(Users, Roles, uow, Tenant, NullLogger<UpdateAdminUser>.Instance);
            Activate = new ActivateAdminUser(Users, uow, Tenant, NullLogger<ActivateAdminUser>.Instance);
            Deactivate = new DeactivateAdminUser(Users, uow, Tenant, NullLogger<DeactivateAdminUser>.Instance);
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
