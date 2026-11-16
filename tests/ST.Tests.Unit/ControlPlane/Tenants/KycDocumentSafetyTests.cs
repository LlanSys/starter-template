using ST.Domain.Features.ControlPlane.Kyc.Entities;
using ST.Domain.Features.ControlPlane.Kyc.Enums;
namespace ST.Tests.Unit.ControlPlane.Tenants;

public sealed class KycDocumentSafetyTests
{
    [Fact]
    public void EvidenceRequiresALiveMatchingScanAndErasureCannotBeReversed()
    {
        var now = DateTimeOffset.Parse("2026-11-14T00:00:00Z");
        var document = new KycDocument(Guid.CreateVersion7(), "Registration", "test.pdf", "application/pdf", 10, new string('A', 64), "test", now);
        Assert.Throws<InvalidOperationException>(() => document.Review(true));
        var first = Guid.CreateVersion7();
        Assert.True(document.TryBeginSafetyScan(first, now, TimeSpan.FromMinutes(3), 5));
        Assert.False(document.TryBeginSafetyScan(Guid.CreateVersion7(), now, TimeSpan.FromMinutes(3), 5));
        Assert.False(document.FinishSafetyScan(Guid.CreateVersion7(), now, KycDocumentSafetyStatus.Clean, "test", "1", now, "clean"));
        Assert.False(document.FinishSafetyScan(first, now.AddMinutes(3), KycDocumentSafetyStatus.Clean, "test", "1", now, "clean"));
        var second = Guid.CreateVersion7();
        Assert.True(document.TryBeginSafetyScan(second, now.AddMinutes(3), TimeSpan.FromMinutes(3), 5));
        Assert.True(document.FinishSafetyScan(second, now.AddMinutes(4), KycDocumentSafetyStatus.Clean, "test", "1", now, "clean"));
        document.Review(true);
        document.BlockChangedContent(now.AddMinutes(5));
        Assert.Equal(KycDocumentSafetyStatus.Blocked, document.SafetyStatus);
        document.QueueSafetyScan(now.AddMinutes(5));
        Assert.Throws<InvalidOperationException>(() => document.Review(true));
        document.EraseMetadata(now.AddMinutes(6));
        Assert.Throws<InvalidOperationException>(() => document.QueueSafetyScan(now.AddMinutes(7)));
        Assert.False(document.FinishSafetyScan(second, now.AddMinutes(7), KycDocumentSafetyStatus.Clean, "test", "1", now, "clean"));
        Assert.Equal(KycDocumentSafetyStatus.Erased, document.SafetyStatus);
    }

    [Fact]
    public void RetriesAreBoundedAndRecoverExpiredLeases()
    {
        var now = DateTimeOffset.Parse("2026-11-14T00:00:00Z");
        var document = new KycDocument(Guid.CreateVersion7(), "Registration", "test.pdf", "application/pdf", 10, new string('A', 64), "test", now);
        Assert.True(document.TryBeginSafetyScan(Guid.CreateVersion7(), now, TimeSpan.FromMinutes(3), 1));
        Assert.False(document.TryBeginSafetyScan(Guid.CreateVersion7(), now.AddMinutes(4), TimeSpan.FromMinutes(3), 1));
        Assert.Equal(KycDocumentSafetyStatus.Failed, document.SafetyStatus);
        Assert.Null(document.NextSafetyScanAt);
        document.QueueSafetyScan(now.AddMinutes(5));
        Assert.True(document.TryBeginSafetyScan(Guid.CreateVersion7(), now.AddMinutes(5), TimeSpan.FromMinutes(3), 1));
    }
}
