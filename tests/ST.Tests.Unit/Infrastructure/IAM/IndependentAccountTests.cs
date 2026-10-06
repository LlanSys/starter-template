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
            var uow = new IamUnitOfWork(Context, Substitute.For<IUserRepository>(), Substitute.For<ISessionRepository>(),
                Substitute.For<ITokenRepository>(), profiles ?? new IamAppUserProfileRepository(Context),
                Substitute.For<IAppUserTotpSecretRepository>(), Substitute.For<ITempTotpSecretRepository>(),
                Substitute.For<IPermissionRepository>(), Substitute.For<IMenuRepository>(), Publisher, NullLogger<IamUnitOfWork>.Instance);
            Create = new CreateAppUser(Users, Roles, uow, Tenant, NullLogger<CreateAppUser>.Instance);
            Update = new UpdateAdminUser(Users, Roles, uow, Tenant, NullLogger<UpdateAdminUser>.Instance);
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

        public override Task<IdentityResult> AddToRolesAsync(AppUser user, IEnumerable<string> roles)
            => RejectRoleChanges
                ? Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "InternalProviderDetail", Code = "TestRejected" }))
                : base.AddToRolesAsync(user, roles);
    }
}
