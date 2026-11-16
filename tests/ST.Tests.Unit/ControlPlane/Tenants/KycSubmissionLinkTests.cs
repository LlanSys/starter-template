using ST.Domain.Features.ControlPlane.Kyc.Entities;

namespace ST.Tests.Unit.ControlPlane.Tenants;

public sealed class KycSubmissionLinkTests
{
    [Fact]
    public void LinkExpiresAtTheExactDeadline()
    {
        var link = new KycSubmissionLink(Guid.CreateVersion7());
        var now = DateTimeOffset.UnixEpoch;
        link.Issue(new string('A', 64), "reviewer", now, TimeSpan.FromHours(1), 2);
        Assert.True(link.IsAvailable(now));
        Assert.False(link.IsAvailable(now.AddHours(1)));
    }

    [Fact]
    public void RotationResetsUploadAllowanceAndChangesConcurrencyAndSubmitterIdentity()
    {
        var link = new KycSubmissionLink(Guid.CreateVersion7());
        link.Issue(new string('A', 64), "reviewer", DateTimeOffset.UnixEpoch, TimeSpan.FromHours(1), 2);
        link.RecordUpload();
        var previousRevision = link.Revision;
        var previousSubmitter = link.SubmitterId;
        link.Issue(new string('B', 64), "reviewer", DateTimeOffset.UnixEpoch, TimeSpan.FromHours(1), 2);
        Assert.Equal(0, link.Uploads);
        Assert.NotEqual(previousRevision, link.Revision);
        Assert.NotEqual(previousSubmitter, link.SubmitterId);
    }

    [Fact]
    public void RevokeClosesTheLinkAndFencesAnInFlightUpload()
    {
        var link = new KycSubmissionLink(Guid.CreateVersion7());
        link.Issue(new string('A', 64), "reviewer", DateTimeOffset.UnixEpoch, TimeSpan.FromHours(1), 1);
        var revision = link.Revision;
        link.Revoke(DateTimeOffset.UnixEpoch.AddMinutes(1));
        Assert.False(link.IsAvailable(DateTimeOffset.UnixEpoch.AddMinutes(2)));
        Assert.NotEqual(revision, link.Revision);
    }

    [Fact]
    public void UploadAllowanceCannotBeExceeded()
    {
        var link = new KycSubmissionLink(Guid.CreateVersion7());
        link.Issue(new string('A', 64), "reviewer", DateTimeOffset.UnixEpoch, TimeSpan.FromHours(1), 1);
        link.RecordUpload();
        Assert.Throws<InvalidOperationException>(() => link.RecordUpload());
        Assert.Equal(1, link.Uploads);
    }
}
