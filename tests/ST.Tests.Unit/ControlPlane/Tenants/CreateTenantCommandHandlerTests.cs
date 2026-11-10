using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.ControlPlane.Tenants.Commands;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Enums;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;
using ST.SharedKernel.Extensions;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace ST.Tests.Unit.ControlPlane.Tenants;

public sealed class CreateTenantCommandHandlerTests
{
    private readonly IControlPlaneUnitOfWork _unitOfWork;
    private readonly IStampProvisioner _provisioner;
    private readonly ILogger<CreateTenantCommandHandler> _logger;
    private readonly IEncryptionService _encryptionService;
    private readonly ICurrentActorProvider _actorProvider;
    private readonly CreateTenantCommandHandler _handler;

    public CreateTenantCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IControlPlaneUnitOfWork>();
        _provisioner = Substitute.For<IStampProvisioner>();
        _logger = Substitute.For<ILogger<CreateTenantCommandHandler>>();
        _encryptionService = Substitute.For<IEncryptionService>();
        _actorProvider = Substitute.For<ICurrentActorProvider>();
        _actorProvider.ActorId.Returns("test-actor-id");
        _handler = new CreateTenantCommandHandler(_unitOfWork, _logger, _encryptionService, _provisioner, _actorProvider, TimeProvider.System);
    }

    [Fact]
    public async Task Handle_WhenTenantAlreadyExists_ReturnsFailure()
    {
        // Arrange
        var request = new CreateTenantRequest
        {
            DisplayName = "Test",
            Identifier = "test-tenant",
            DeploymentStampId = Guid.NewGuid(),
            SubscriptionTier = "Enterprise",
            HostName = "test.example.com"
        };
        var command = new CreateTenantCommand(request);

        _unitOfWork.Tenants.AnyAsync(
            Arg.Any<Expression<Func<Tenant, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("already exists", result.Message, StringComparison.OrdinalIgnoreCase);
        await _unitOfWork.Tenants.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().CompleteAsync(default);
    }

    [Fact]
    public async Task Handle_WhenStampDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var request = new CreateTenantRequest
        {
            DisplayName = "Test",
            Identifier = "test-tenant",
            DeploymentStampId = Guid.NewGuid(),
            SubscriptionTier = "Enterprise",
            HostName = "test.example.com"
        };
        var command = new CreateTenantCommand(request);

        _unitOfWork.Tenants.AnyAsync(
            Arg.Any<Expression<Func<Tenant, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(false);

        _unitOfWork.DeploymentStamps.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<DeploymentStamp, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns((DeploymentStamp)null!);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("does not exist", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WhenSubscriptionTierIsInvalid_ReturnsFailure()
    {
        // Arrange
        var request = new CreateTenantRequest
        {
            DisplayName = "Test",
            Identifier = "test-tenant",
            DeploymentStampId = Guid.NewGuid(),
            SubscriptionTier = "InvalidTier123",
            HostName = "test.example.com"
        };
        var command = new CreateTenantCommand(request);

        _unitOfWork.Tenants.AnyAsync(
            Arg.Any<Expression<Func<Tenant, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(false);

        _unitOfWork.DeploymentStamps.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<DeploymentStamp, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(new DeploymentStamp { Id = request.DeploymentStampId, Name = "Stamp", TargetResourceGroup = "test-rg", CreatedBy = "test" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid Subscription Tier", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithValidRequest_CreatesTenantWithPendingApprovalStatus()
    {
        // Arrange
        var stampId = Guid.NewGuid();
        var request = new CreateTenantRequest
        {
            DisplayName = "Test Tenant",
            Identifier = "test-tenant",
            DeploymentStampId = stampId,
            SubscriptionTier = "Enterprise",
            ContactEmail = "test@example.com",
            DatabaseProvider = "SqlServer",
            HostName = "test.example.com",
            DatabaseConnectionString = "Server=localhost;Database=db;"
        };
        var command = new CreateTenantCommand(request);

        var stamp = new DeploymentStamp
        {
            Id = stampId,
            Name = "Stamp",
            IsolationTier = IsolationTier.Pooled,
            TargetResourceGroup = "test-rg",
            CreatedBy = "test-user"
        };

        _unitOfWork.DeploymentStamps.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<DeploymentStamp, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(stamp);

        _unitOfWork.Tenants.AnyAsync(
            Arg.Any<Expression<Func<Tenant, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(false);

        _encryptionService.Encrypt(Arg.Any<string>()).Returns("encrypted-string");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TenantStatus.PendingApproval.ToDisplayString(), result.Data!.Status);

        await _unitOfWork.Tenants.Received(1).CreateAsync(
            Arg.Is<Tenant>(t =>
                t.Status == TenantStatus.PendingApproval &&
                t.Identifier == "test-tenant" &&
                t.DatabaseConnectionString == "encrypted-string"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());

        // Ensure provisioner is NOT called directly from this handler anymore
        await _provisioner.DidNotReceiveWithAnyArgs().ProvisionIsolatedStampAsync(
            default!, default!, default!, default!, default);
    }
}
