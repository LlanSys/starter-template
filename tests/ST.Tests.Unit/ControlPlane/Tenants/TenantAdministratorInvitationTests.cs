using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Enums;

namespace ST.Tests.Unit.ControlPlane.Tenants;

public sealed class TenantAdministratorInvitationTests
{
    [Fact]
    public void LeaseExpiryAndResendPreventOldCredentialsFromBeingClaimed()
    {
        var now = DateTimeOffset.UnixEpoch;
        var invitation = new TenantAdministratorInvitation(Guid.CreateVersion7(), "admin@example.test",
            "Tenant", "Owner", "reviewer", now);

        Assert.True(invitation.TryIssue("first-hash", now, TimeSpan.FromHours(48)));
        Assert.False(invitation.TryIssue("another-hash", now.AddMinutes(4), TimeSpan.FromHours(48)));
        invitation.ReportDelivery(true, now.AddMinutes(1));
        Assert.False(invitation.Reserve("account", now.AddHours(48)));
        Assert.True(invitation.Resend("operator", now.AddHours(49)));
        Assert.Null(invitation.TokenHash);
        Assert.Null(invitation.ExpiresAt);
        Assert.True(invitation.TryIssue("second-hash", now.AddHours(49), TimeSpan.FromHours(48)));
        Assert.True(invitation.Reserve("account", now.AddHours(49)));
        Assert.False(invitation.Reserve("different-account", now.AddHours(49)));
        invitation.Claim();

        Assert.Null(invitation.TokenHash);
        Assert.False(invitation.Reserve("account", now.AddHours(49)));
        Assert.True(invitation.Accept("account", now.AddHours(50)));
        Assert.Equal(AdministratorInvitationState.Accepted, invitation.State);
        Assert.False(invitation.Revoke("operator", now.AddHours(51)));
    }
}
