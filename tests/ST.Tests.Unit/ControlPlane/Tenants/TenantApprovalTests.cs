using System.Reflection;
using ST.Api.Common.Authorization;
using ST.Api.Features.ControlPlane.Tenants.Controllers;
using ST.Application.Features.ControlPlane.Tenants.Commands;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ST.Tests.Unit.ControlPlane.Tenants;

public sealed class TenantApprovalTests
{
    [Fact]
    public void ApprovalRequiresActorAndRecordsTransition()
    {
        var tenant = CreateTenant();
        Assert.Equal(TenantStatus.PendingApproval, tenant.Status);
        Assert.Throws<ArgumentException>(() => tenant.Approve(" ", DateTimeOffset.UnixEpoch, "review-1"));
        Assert.Equal(TenantStatus.PendingApproval, tenant.Status);
        tenant.Approve("administrator-id", DateTimeOffset.UnixEpoch, "review-1");
        Assert.Equal(TenantStatus.PendingProvisioning, tenant.Status);
        Assert.Equal("administrator-id", tenant.UpdatedBy);
        Assert.Equal(DateTimeOffset.UnixEpoch, tenant.UpdatedAt);
        Assert.Equal(DateTimeOffset.UnixEpoch, tenant.KycApprovedAt);
        Assert.Equal("review-1", tenant.KycEvidenceReference);
        Assert.Equal("administrator-id", tenant.KycApprovedBy);
        Assert.Throws<InvalidOperationException>(() => tenant.Approve("other-id", DateTimeOffset.UnixEpoch, "review-2"));
    }

    [Fact]
    public void UnapprovedTenantCannotBecomeActiveThroughSuspensionOrProvisioning()
    {
        var tenant = CreateTenant();
        Assert.Throws<InvalidOperationException>(tenant.MarkAsActive);
        Assert.Throws<InvalidOperationException>(tenant.Suspend);
        Assert.Throws<InvalidOperationException>(tenant.MarkAsProvisioning);
        Assert.Equal(TenantStatus.PendingApproval, tenant.Status);
        tenant.Approve("administrator-id", DateTimeOffset.UnixEpoch, "review-1");
        tenant.MarkAsProvisioning();
        tenant.MarkAsActive();
        tenant.Suspend();
        tenant.MarkAsActive();
        Assert.Equal(TenantStatus.Active, tenant.Status);
    }

    [Theory]
    [InlineData(TenantStatus.PendingApproval)]
    [InlineData(TenantStatus.PendingProvisioning)]
    [InlineData(TenantStatus.Provisioning)]
    [InlineData(TenantStatus.ProvisioningFailed)]
    [InlineData(TenantStatus.Active)]
    [InlineData(TenantStatus.Suspended)]
    public async Task ActivateEndpointHandlerCannotBypassApprovalOrProvisioning(TenantStatus status)
    {
        var tenant = CreateTenant();
        tenant.Status = status;
        var uow = Substitute.For<IControlPlaneUnitOfWork>();
        uow.Tenants.FindByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        var actor = Substitute.For<ICurrentActorProvider>();
        actor.ActorId.Returns("administrator-id");
        var handler = new ActivateTenantCommandHandler(uow, NullLogger<ActivateTenantCommandHandler>.Instance, actor, TimeProvider.System);
        var result = await handler.Handle(new ActivateTenantCommand(tenant.Id), CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(status, tenant.Status);
        await uow.DidNotReceiveWithAnyArgs().CompleteAsync(default);
    }

    [Theory]
    [InlineData(TenantStatus.PendingApproval)]
    [InlineData(TenantStatus.PendingProvisioning)]
    [InlineData(TenantStatus.Provisioning)]
    [InlineData(TenantStatus.ProvisioningFailed)]
    [InlineData(TenantStatus.Suspended)]
    public async Task SuspendEndpointHandlerCannotManufacturePreviouslyApprovedState(TenantStatus status)
    {
        var tenant = CreateTenant();
        tenant.Status = status;
        var uow = Substitute.For<IControlPlaneUnitOfWork>();
        uow.Tenants.FindByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        var actor = Substitute.For<ICurrentActorProvider>();
        var handler = new SuspendTenantCommandHandler(uow, NullLogger<SuspendTenantCommandHandler>.Instance, actor, TimeProvider.System);
        var result = await handler.Handle(new SuspendTenantCommand(tenant.Id), CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(status, tenant.Status);
        await uow.DidNotReceiveWithAnyArgs().CompleteAsync(default);
    }

    [Fact]
    public async Task SuspendedTenantCanReactivateWithStableAuditActor()
    {
        var tenant = CreateTenant();
        tenant.Approve("reviewer-id", DateTimeOffset.UnixEpoch, "review-1");
        tenant.MarkAsActive();
        tenant.Suspend();
        var uow = Substitute.For<IControlPlaneUnitOfWork>();
        uow.Tenants.FindByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);
        var actor = Substitute.For<ICurrentActorProvider>();
        actor.ActorId.Returns("administrator-id");
        var handler = new ActivateTenantCommandHandler(uow, NullLogger<ActivateTenantCommandHandler>.Instance, actor, TimeProvider.System);
        using var cancellation = new CancellationTokenSource();
        var result = await handler.Handle(new ActivateTenantCommand(tenant.Id), cancellation.Token);
        Assert.True(result.IsSuccess);
        Assert.Equal(TenantStatus.Active, tenant.Status);
        Assert.Equal("administrator-id", tenant.UpdatedBy);
        await uow.Received(1).CompleteAsync(cancellation.Token);
    }

    [Theory]
    [InlineData(nameof(TenantsController.Approve))]
    [InlineData(nameof(TenantsController.RetryProvisioning))]
    [InlineData(nameof(TenantsController.ActivateTenant))]
    [InlineData(nameof(TenantsController.SuspendTenant))]
    public void AdministrativeTransitionsRequireAuthenticationAndControlPlanePermission(string methodName)
    {
        Assert.NotNull(typeof(TenantsController).GetCustomAttribute<AuthorizeAttribute>());
        var method = typeof(TenantsController).GetMethod(methodName)!;
        Assert.Null(method.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Equal(PermissionAuthorizationConstants.BuildPolicyName(methodName == nameof(TenantsController.Approve)
            ? "control_plane.approve_kyc" : "control_plane.manage"),
            method.GetCustomAttribute<RequirePermissionAttribute>()!.Policy);
    }

    private static Tenant CreateTenant() => new()
    {
        Id = Guid.CreateVersion7(),
        Identifier = "test",
        DisplayName = "Test tenant",
        HostName = "test.local",
        CreatedBy = "creator-id"
    };
}
