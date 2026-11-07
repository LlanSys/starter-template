using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Infrastructure.Security;
using ST.Persistence.Features.ControlPlane.DataContext;
using Microsoft.Extensions.DependencyInjection;
using ST.Application.Features.IAM.Users.Commands;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration.ControlPlane;

[Collection("Starter API")]
public sealed class ExternalSignInTests(StarterTemplateWebApplicationFactory factory)
{
    [Fact]
    public async Task ProviderInvitationCreatesRestrictedIdentityOnlyAfterExplicitEligibleAcceptance()
    {
        var tenantId = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(tenantId);
        var email = $"provider-invite-{tenantId:N}@example.test";
        var token = SingleUseCredential.Create();
        var now = DateTimeOffset.UtcNow;
        Guid invitationId;
        using (var scope = factory.Services.CreateScope())
        {
            var control = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
            var tenant = await control.Tenants.SingleAsync(item => item.Id == tenantId);
            tenant.WorkspaceVersion = 1;
            tenant.Suspend();
            var invitation = new TenantAdministratorInvitation(tenantId, email, "External", "Administrator", "reviewer", now);
            Assert.True(invitation.TryIssue(SingleUseCredential.Hash(token), now, TimeSpan.FromHours(1)));
            invitation.ReportDelivery(true, now);
            invitationId = invitation.Id;
            control.AdministratorInvitations.Add(invitation);
            await control.SaveChangesAsync();
        }

        var request = new ProcessSsoLoginCommand("Google", "invited-provider-subject", invitationId, token, email);
        using (var scope = factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = tenantId;
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            Assert.False((await sender.Send(request)).IsSuccess);
            Assert.Null(await scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>().FindByEmailAsync(email));
        }

        using (var scope = factory.Services.CreateScope())
        {
            var control = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
            var tenant = await control.Tenants.SingleAsync(item => item.Id == tenantId);
            Assert.Equal(TenantStatus.Suspended, tenant.Status);
            tenant.MarkAsActive();
            await control.SaveChangesAsync();
        }

        string code;
        using (var scope = factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = tenantId;
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            Assert.False((await sender.Send(request with { VerifiedEmail = "different@example.test" })).IsSuccess);
            var result = await sender.Send(request);
            Assert.True(result.IsSuccess, result.Message);
            code = result.Data!;
            Assert.False((await sender.Send(request)).IsSuccess);

            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var account = await users.FindByEmailAsync(email);
            Assert.NotNull(account);
            Assert.True(account.RequiresAdministratorSetup);
            Assert.False(await users.HasPasswordAsync(account));
            Assert.Empty(await users.GetRolesAsync(account));
        }

        using (var scope = factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = tenantId;
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var session = await sender.Send(new ExchangeSsoCodeCommand(code));
            Assert.True(session.IsSuccess, session.Message);
            Assert.True(session.Data!.MfaEnrollmentRequired);
            Assert.DoesNotContain(session.Data.UserClaims!, claim => claim.Type == "permission");
        }
    }

    [Fact]
    public async Task EstablishedIdentityUsesTenantBoundOneTimeExchangeAndApplicationMfa()
    {
        var tenant = Guid.CreateVersion7();
        var otherTenant = Guid.CreateVersion7();
        await factory.RegisterTenantAsync(tenant);
        await factory.RegisterTenantAsync(otherTenant);

        string code;
        using (var scope = factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = tenant;
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = AppUser.CreateAccount(tenant, $"sso-{tenant:N}", "External", "Account", $"sso-{tenant:N}@example.test", null, "test-vendor");
            user.IsActive = true;
            user.EmailConfirmed = true;
            user.TwoFactorEnabled = true;
            Assert.True((await manager.CreateAsync(user, "ExternalTest42!")).Succeeded);

            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var unlinked = await sender.Send(new ProcessSsoLoginCommand("Google", "unlinked-subject", VerifiedEmail: user.Email));
            Assert.False(unlinked.IsSuccess);
            Assert.Empty(await manager.GetLoginsAsync(user));

            Assert.True((await manager.AddLoginAsync(user, new UserLoginInfo("Google", "established-subject", "Google"))).Succeeded);
            var result = await sender.Send(new ProcessSsoLoginCommand("Google", "established-subject"));
            Assert.True(result.IsSuccess, result.Message);
            code = result.Data!;
        }

        using (var scope = factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = otherTenant;
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            Assert.False((await sender.Send(new ExchangeSsoCodeCommand(code))).IsSuccess);
        }

        using (var scope = factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = tenant;
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var challenge = await sender.Send(new ExchangeSsoCodeCommand(code));
            Assert.True(challenge.IsSuccess, challenge.Message);
            Assert.True(challenge.Data!.Requires2FA);
            Assert.False(challenge.Data.IsAuthenticated);
            Assert.Empty(challenge.Data.RefreshToken);
            Assert.False((await sender.Send(new ExchangeSsoCodeCommand(code))).IsSuccess);
        }
    }
}
