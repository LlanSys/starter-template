using ST.Domain.Features.ControlPlane.Kyc.Entities;
namespace ST.Tests.Unit.ControlPlane.Tenants;

public sealed class KycRetentionTests
{
    private static readonly DateTimeOffset Start = new(2026, 1, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ApprovedCaseHasNoExpiryUntilRelationshipEndAndUsesCalendarMonths()
    {
        var policy = new KycRetentionPolicy { ApprovedMonths = 1 };
        var item = new KycRetentionCase(Guid.CreateVersion7(), policy.Id, Start);
        item.Schedule(policy, true);
        Assert.Null(item.RetainUntil);
        Assert.Throws<InvalidOperationException>(() => item.Close("Declined", Start, policy, true));
        item.Close("Ended", Start, policy, true);
        Assert.Equal(Start.AddMonths(1), item.RetainUntil);
        Assert.False(item.CanCollect);
    }

    [Theory]
    [InlineData("Declined")]
    [InlineData("Withdrawn")]
    public void ClosedUnsuccessfulCaseRetainsForNinetyDays(string outcome)
    {
        var policy = new KycRetentionPolicy();
        var item = new KycRetentionCase(Guid.CreateVersion7(), policy.Id, Start);
        item.Close(outcome, Start, policy, false);
        Assert.Equal(Start.AddDays(90), item.RetainUntil);
        Assert.Throws<InvalidOperationException>(() => item.Touch(Start));
    }

    [Fact]
    public void HoldNeverExpiresAutomaticallyAndRecoveryIsReversibleUntilDeletionCommits()
    {
        var policy = new KycRetentionPolicy();
        var item = new KycRetentionCase(Guid.CreateVersion7(), policy.Id, Start);
        item.Schedule(policy, false);
        item.SetHold(true, Start, policy);
        Assert.Equal(Start.AddDays(90), item.HoldReviewDue);
        Assert.Throws<InvalidOperationException>(() => item.BeginRecovery(Start.AddYears(5), policy));
        item.SetHold(false, Start.AddDays(31), policy);
        item.BeginRecovery(Start.AddDays(31), policy);
        Assert.Equal("Abandoned", item.Outcome);
        Assert.Equal(Start.AddDays(61), item.RecoveryUntil);
        item.Restore(Start.AddDays(32), policy, false);
        Assert.Equal("Open", item.Outcome);
        Assert.Equal(Start.AddDays(62), item.RetainUntil);
        item.BeginRecovery(Start.AddDays(62), policy);
        Assert.Throws<InvalidOperationException>(() => item.ClaimDeletion(Start.AddDays(91)));
        item.ClaimDeletion(Start.AddDays(92));
        Assert.Throws<InvalidOperationException>(() => item.SetHold(true, Start.AddDays(92), policy));
        Assert.Throws<InvalidOperationException>(() => item.Restore(Start.AddDays(92), policy, false));
    }

    [Fact]
    public void RetryLeasesFenceCompetingWorkersAndPolicyChangesKeepOriginalStartingEvent()
    {
        var original = new KycRetentionPolicy();
        var item = new KycRetentionCase(Guid.CreateVersion7(), original.Id, Start);
        item.Close("Declined", Start, original, false);
        var replacement = new KycRetentionPolicy { ClosedDays = 180 };
        item.ApplyPolicy(replacement, false);
        Assert.Equal(Start.AddDays(180), item.RetainUntil);
        Assert.Equal(replacement.Id, item.PolicyId);
        item.BeginRecovery(Start.AddDays(180), replacement);
        item.ClaimDeletion(Start.AddDays(210));
        Assert.Throws<InvalidOperationException>(() => item.ClaimDeletion(Start.AddDays(210)));
        item.FinishAttempt(false, "StorageUnavailable", Start.AddDays(210));
        Assert.Equal("Deleting", item.Phase);
        Assert.Equal("StorageUnavailable", item.FailureCode);
        item.ClaimDeletion(Start.AddDays(211));
        item.FinishAttempt(true, null, Start.AddDays(211));
        Assert.Equal("Erased", item.Phase);
    }
}
