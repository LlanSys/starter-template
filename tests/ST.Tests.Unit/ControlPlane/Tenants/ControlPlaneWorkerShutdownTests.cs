using Microsoft.Extensions.Options;
using ST.Application.Features.ControlPlane.Kyc.Contracts;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Features.ControlPlane.Kyc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ST.Infrastructure.Features.ControlPlane.Provisioning;
using ST.Infrastructure.Features.ControlPlane.Tenants.Services;

namespace ST.Tests.Unit.ControlPlane.Tenants;

public sealed class ControlPlaneWorkerShutdownTests
{
    [Fact]
    public async Task InvitationWorkerCompletesWhenStoppedWhileWaitingForNextSweep()
    {
        var clock = new PausedTimeProvider();
        using var worker = new AdministratorInvitationWorker(
            Substitute.For<IServiceScopeFactory>(),
            clock,
            NullLogger<AdministratorInvitationWorker>.Instance);

        await AssertCleanShutdownAsync(worker, clock);
    }

    [Fact]
    public async Task ProvisioningWorkerCompletesWhenStoppedWhileWaitingForNextSweep()
    {
        var clock = new PausedTimeProvider();
        using var worker = new ProvisioningTimeoutWorker(
            Substitute.For<IServiceScopeFactory>(),
            clock,
            NullLogger<ProvisioningTimeoutWorker>.Instance);

        await AssertCleanShutdownAsync(worker, clock);
    }

    [Fact]
    public async Task RetentionWorkerCompletesWhenStoppedWhileWaitingForNextSweep()
    {
        var clock = new PausedTimeProvider();
        using var worker = new KycRetentionWorker(Substitute.For<IServiceScopeFactory>(), clock, NullLogger<KycRetentionWorker>.Instance);
        await AssertCleanShutdownAsync(worker, clock);
    }

    [Fact]
    public async Task SafetyWorkerCompletesWhenStoppedWhileWaitingForNextScan()
    {
        var clock = new PausedTimeProvider();
        using var worker = new KycDocumentSafetyWorker(Substitute.For<IKycDocumentSafetyProcessor>(),
            Options.Create(new KycDocumentSafetySettings()), clock, NullLogger<KycDocumentSafetyWorker>.Instance);

        await AssertCleanShutdownAsync(worker, clock);
    }

    private static async Task AssertCleanShutdownAsync(BackgroundService worker, PausedTimeProvider clock)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await worker.StartAsync(CancellationToken.None);
        await clock.TimerCreated.Task.WaitAsync(timeout.Token);

        await worker.StopAsync(timeout.Token);

        var execution = Assert.IsAssignableFrom<Task>(worker.ExecuteTask);
        Assert.True(execution.IsCompletedSuccessfully, $"Worker finished with status {execution.Status}.");
    }

    private sealed class PausedTimeProvider : TimeProvider
    {
        public TaskCompletionSource TimerCreated { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = base.CreateTimer(callback, state, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            TimerCreated.SetResult();
            return timer;
        }
    }
}
