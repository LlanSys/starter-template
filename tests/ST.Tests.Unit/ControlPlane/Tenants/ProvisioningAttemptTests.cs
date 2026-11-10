using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.ControlPlane.Tenants.Commands;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.SharedKernel.Dtos.Common;

namespace ST.Tests.Unit.ControlPlane.Tenants;

public sealed class ProvisioningAttemptTests
{
    [Fact]
    public async Task CompletionIsBoundToTheCurrentAttemptAndVerifiedDestination()
    {
        var now = DateTimeOffset.UtcNow;
        var tenant = new Tenant { Identifier = "attempt", DisplayName = "Attempt", HostName = "attempt.invalid", CreatedBy = "reviewer" };
        tenant.Approve("reviewer", now, "review");
        var first = tenant.BeginProvisioning(now, TimeSpan.FromHours(2));
        var uow = Substitute.For<IControlPlaneUnitOfWork>();
        var readiness = Substitute.For<ITenantDatabaseReadiness>();
        var encryption = Substitute.For<IEncryptionService>();
        encryption.Encrypt(Arg.Any<string>()).Returns(call => "protected-" + call.Arg<string>());
        encryption.Decrypt(Arg.Any<string>()).Returns(call => call.Arg<string>()[10..]);
        uow.Tenants.FindByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        uow.DeploymentStamps.FindByIdAsync(tenant.DeploymentStampId, Arg.Any<CancellationToken>())
            .Returns(new DeploymentStamp { Name = "isolated", TargetResourceGroup = "test", IsolationTier = IsolationTier.Isolated, DatabaseProvider = "SqlServer", CreatedBy = "fixture" });
        using var services = new ServiceCollection().AddLogging()
            .AddSingleton(uow).AddSingleton(readiness).AddSingleton(encryption).AddSingleton(TimeProvider.System)
            .AddMediatR(configuration => configuration.RegisterServicesFromAssemblyContaining<CompleteTenantProvisioningCommand>())
            .BuildServiceProvider();
        var sender = services.GetRequiredService<ISender>();

        var stale = await sender.Send(new CompleteTenantProvisioningCommand(tenant.Id, "destination", "", Guid.CreateVersion7()));
        Assert.False(stale.IsSuccess);
        await readiness.DidNotReceiveWithAnyArgs().VerifyAsync(default, default!, default!, default);
        Assert.Equal(TenantStatus.Provisioning, tenant.Status);

        foreach (var origin in new[] { "http://external.example.test", "https://user@example.test", "https://example.test/path", "https://example.test/?token=value", "https://example.test/#token" })
        {
            Assert.False((await sender.Send(new CompleteTenantProvisioningCommand(
                tenant.Id, "destination", "", first, ClientOrigin: origin))).IsSuccess);
        }

        await readiness.DidNotReceiveWithAnyArgs().VerifyAsync(default, default!, default!, default);
        readiness.VerifyAsync(tenant.Id, "destination", "SqlServer", Arg.Any<CancellationToken>())
            .Returns(AppResponses.Failure<bool>("Unapplied migrations"));
        var rejected = await sender.Send(new CompleteTenantProvisioningCommand(tenant.Id, "destination", "", first));
        Assert.False(rejected.IsSuccess);
        Assert.Equal(TenantStatus.Provisioning, tenant.Status);
        Assert.Null(tenant.DatabaseConnectionString);

        readiness.VerifyAsync(tenant.Id, "destination", "SqlServer", Arg.Any<CancellationToken>())
            .Returns(AppResponses.Success(true));
        Assert.True((await sender.Send(new CompleteTenantProvisioningCommand(tenant.Id, "destination", "", first))).IsSuccess);
        Assert.Equal(TenantStatus.Active, tenant.Status);
        Assert.True((await sender.Send(new CompleteTenantProvisioningCommand(tenant.Id, "destination", "", first))).IsSuccess);
        Assert.False((await sender.Send(new CompleteTenantProvisioningCommand(tenant.Id, "other", "", first))).IsSuccess);
        Assert.True((await sender.Send(new CompleteTenantProvisioningCommand(tenant.Id, "", "", first, false))).IsSuccess);
        Assert.Equal(TenantStatus.Active, tenant.Status);
        await uow.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void WorkspaceSetupLeaseFencesConcurrentAndExpiredOwners()
    {
        var now = DateTimeOffset.UnixEpoch;
        var tenant = new Tenant { Identifier = "setup", DisplayName = "Setup", HostName = "setup.invalid", CreatedBy = "reviewer" };
        tenant.Approve("reviewer", now, "review");
        tenant.MarkAsActive();
        var first = Guid.CreateVersion7();
        var second = Guid.CreateVersion7();
        Assert.True(tenant.TryBeginWorkspaceSetup(first, now));
        Assert.False(tenant.TryBeginWorkspaceSetup(second, now.AddSeconds(1)));
        Assert.True(tenant.TryBeginWorkspaceSetup(second, now.AddMinutes(3)));
        Assert.Throws<InvalidOperationException>(() => tenant.CompleteWorkspaceSetup(first, now.AddMinutes(3)));
        tenant.CompleteWorkspaceSetup(second, now.AddMinutes(3).AddSeconds(1));
        Assert.Equal(1, tenant.WorkspaceVersion);
        Assert.Null(tenant.WorkspaceSetupLeaseId);
        Assert.Null(tenant.WorkspaceSetupLeaseUntil);
    }

    [Fact]
    public void TimeoutRequiresANewAttemptAndPreservesKyc()
    {
        var now = DateTimeOffset.UnixEpoch;
        var tenant = new Tenant { Identifier = "retry", DisplayName = "Retry", HostName = "retry.invalid", CreatedBy = "reviewer" };
        tenant.Approve("reviewer", now, "review");
        var first = tenant.BeginProvisioning(now, TimeSpan.FromHours(2));
        Assert.Throws<InvalidOperationException>(() => tenant.RetryProvisioning(now.AddHours(1)));
        tenant.RetryProvisioning(now.AddHours(3));
        var second = tenant.BeginProvisioning(now.AddHours(3), TimeSpan.FromHours(2));
        Assert.NotEqual(first, second);
        Assert.Equal(now, tenant.KycApprovedAt);
        Assert.Equal("review", tenant.KycEvidenceReference);
    }
}
