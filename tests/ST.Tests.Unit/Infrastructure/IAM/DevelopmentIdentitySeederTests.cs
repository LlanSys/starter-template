using System.ComponentModel.DataAnnotations;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Features.IAM.Users.Enums;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Features.IAM.Users.Seeding;
using ST.Persistence.Features.IAM.DataContext;
using ST.SharedKernel.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace ST.Tests.Unit.Infrastructure.IAM;

public sealed class DevelopmentIdentitySeederTests
{
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private const string UserName = "bootstrap.admin";
    private const string Email = "admin@example.test";
    private const string Password = "Test-only-bootstrap-123!";

    [Fact]
    public async Task DisabledSeedDoesNotReadUsersOrCreateRoles()
    {
        using var fixture = new Fixture(new DevelopmentSeedSettings());
        await fixture.Seeder.SeedAsync();
        Assert.Empty(fixture.Users.ReceivedCalls());
        Assert.Empty(fixture.Roles.ReceivedCalls());
    }

    [Fact]
    public async Task EnabledSeedRequiresExplicitCredentialsBeforeWriting()
    {
        using var fixture = new Fixture(new DevelopmentSeedSettings { Enabled = true, TenantId = TenantId });
        await Assert.ThrowsAsync<ValidationException>(() => fixture.Seeder.SeedAsync());
        await fixture.Roles.DidNotReceiveWithAnyArgs().CreateAsync(default!);
        await fixture.Users.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!);
    }

    [Fact]
    public async Task NewAdministratorUsesConfiguredIdentityWithoutBusinessLinks()
    {
        using var fixture = new Fixture(Settings());
        AppUser? created = null;
        fixture.Users.CreateAsync(Arg.Any<AppUser>(), Password).Returns(call =>
        {
            created = call.ArgAt<AppUser>(0);
            return Task.FromResult(IdentityResult.Success);
        });

        await fixture.Seeder.SeedAsync();

        Assert.NotNull(created);
        Assert.Equal(UserName, created.UserName);
        Assert.Equal(Email, created.Email);
        Assert.Equal(TenantId, created.TenantId);
        Assert.Null(created.EmployeeId);
        Assert.Null(created.CustomerId);
        Assert.True(created.IsActive);
        Assert.True(created.EmailConfirmed);
        Assert.False(created.PhoneNumberConfirmed);
        Assert.True(Guid.TryParse(created.CreatedBy, out _));
        Assert.Equal(created.CreatedBy, created.ActivatedBy);
        await fixture.Roles.Received(1).CreateAsync(Arg.Is<AppRole>(role => role.Name == Roles.SysAdmin.ToDisplayString()));
        await fixture.Users.Received(1).AddToRoleAsync(created, Roles.SysAdmin.ToDisplayString());
        await fixture.Users.DidNotReceiveWithAnyArgs().DeleteAsync(default!);
    }

    [Fact]
    public async Task ExistingAdministratorKeepsPasswordAndLifecycleOnRerun()
    {
        var admin = ExistingAdmin();
        using var fixture = new Fixture(Settings(), admin);
        fixture.Users.IsInRoleAsync(admin, Roles.SysAdmin.ToDisplayString()).Returns(true);
        fixture.Roles.RoleExistsAsync(Roles.SysAdmin.ToDisplayString()).Returns(true);

        await fixture.Seeder.SeedAsync();

        await fixture.Users.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!);
        await fixture.Users.DidNotReceiveWithAnyArgs().UpdateAsync(default!);
        await fixture.Users.DidNotReceiveWithAnyArgs().ResetPasswordAsync(default!, default!, default!);
        Assert.Equal("Original Actor", admin.CreatedBy);
    }

    [Theory]
    [InlineData("tenant")]
    [InlineData("employee")]
    [InlineData("customer")]
    [InlineData("disabled")]
    [InlineData("deleted")]
    [InlineData("username")]
    public async Task ConflictingExistingAccountIsNotPromotedOrReactivated(string conflict)
    {
        var admin = ExistingAdmin();
        switch (conflict)
        {
            case "tenant": admin.TenantId = Guid.CreateVersion7(); break;
            case "employee": admin.EmployeeId = Guid.CreateVersion7(); break;
            case "customer": admin.CustomerId = Guid.CreateVersion7(); break;
            case "disabled": admin.IsActive = false; break;
            case "deleted": admin.IsDeleted = true; break;
            case "username": admin.UserName = "another.account"; break;
        }
        using var fixture = new Fixture(Settings(), admin);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Seeder.SeedAsync());

        await fixture.Roles.DidNotReceiveWithAnyArgs().CreateAsync(default!);
        await fixture.Users.DidNotReceiveWithAnyArgs().AddToRoleAsync(default!, default!);
        await fixture.Users.DidNotReceiveWithAnyArgs().UpdateAsync(default!);
        await fixture.Users.DidNotReceiveWithAnyArgs().DeleteAsync(default!);
    }

    [Fact]
    public async Task UnrelatedAccountsAreNeverDeletedByBootstrap()
    {
        var unrelated = ExistingAdmin();
        unrelated.UserName = "other";
        unrelated.NormalizedUserName = "OTHER";
        unrelated.Email = "other@startertemplate.local";
        unrelated.NormalizedEmail = "OTHER@STARTERTEMPLATE.LOCAL";
        using var fixture = new Fixture(Settings(), unrelated);

        await fixture.Seeder.SeedAsync();

        await fixture.Users.DidNotReceiveWithAnyArgs().DeleteAsync(default!);
        Assert.True(unrelated.IsActive);
    }

    [Fact]
    public async Task ExplicitPasswordResetUsesIdentityTokenAndChecksResult()
    {
        var admin = ExistingAdmin();
        using var fixture = new Fixture(Settings(resetPassword: true), admin);
        fixture.Users.GeneratePasswordResetTokenAsync(admin).Returns("reset-token");
        fixture.Users.ResetPasswordAsync(admin, "reset-token", Password).Returns(IdentityResult.Failed(new IdentityError { Code = "Rejected" }));

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Seeder.SeedAsync());
        await fixture.Users.DidNotReceiveWithAnyArgs().AddToRoleAsync(default!, default!);
    }

    private static DevelopmentSeedSettings Settings(bool resetPassword = false) => new()
    {
        Enabled = true,
        TenantId = TenantId,
        AdminUserName = UserName,
        AdminEmail = Email,
        AdminPassword = Password,
        ResetExistingAdminPassword = resetPassword
    };

    private static AppUser ExistingAdmin() => new()
    {
        Id = Guid.CreateVersion7().ToString(),
        TenantId = TenantId,
        UserName = UserName,
        NormalizedUserName = UserName.ToUpperInvariant(),
        Email = Email,
        NormalizedEmail = Email.ToUpperInvariant(),
        IsActive = true,
        CreatedBy = "Original Actor"
    };

    private sealed class Fixture : IDisposable
    {
        private readonly IamDBContext _context;
        public UserManager<AppUser> Users { get; }
        public RoleManager<AppRole> Roles { get; }
        public DevelopmentIdentitySeeder Seeder { get; }

        public Fixture(DevelopmentSeedSettings settings, params AppUser[] users)
        {
            _context = new IamDBContext(new DbContextOptionsBuilder<IamDBContext>()
                .UseInMemoryDatabase(Guid.CreateVersion7().ToString()).Options);
            _context.Users.AddRange(users);
            _context.SaveChanges();
            Users = Substitute.For<UserManager<AppUser>>(
                Substitute.For<IUserStore<AppUser>>(), Options.Create(new IdentityOptions()),
                new PasswordHasher<AppUser>(), Array.Empty<IUserValidator<AppUser>>(),
                Array.Empty<IPasswordValidator<AppUser>>(), new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(), null!, NullLogger<UserManager<AppUser>>.Instance);
            Users.Users.Returns(_context.Users);
            Users.NormalizeName(Arg.Any<string>()).Returns(call => call.Arg<string>().ToUpperInvariant());
            Users.NormalizeEmail(Arg.Any<string>()).Returns(call => call.Arg<string>().ToUpperInvariant());
            Users.CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);
            Users.AddToRoleAsync(Arg.Any<AppUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);
            Roles = Substitute.For<RoleManager<AppRole>>(
                Substitute.For<IRoleStore<AppRole>>(), Array.Empty<IRoleValidator<AppRole>>(),
                new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), NullLogger<RoleManager<AppRole>>.Instance);
            Roles.CreateAsync(Arg.Any<AppRole>()).Returns(IdentityResult.Success);
            Seeder = new DevelopmentIdentitySeeder(Roles, Users, Options.Create(settings), NullLogger<DevelopmentIdentitySeeder>.Instance);
            Users.ClearReceivedCalls();
            Roles.ClearReceivedCalls();
        }

        public void Dispose()
        {
            Users.Dispose();
            Roles.Dispose();
            _context.Dispose();
        }
    }
}
