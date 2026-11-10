using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Quartz;
using ST.Infrastructure.Jobs;
using ST.Infrastructure.Configuration;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Tenants.Contracts.Repositories;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Shared.Contracts.Common;
using System.Linq.Expressions;
using System.Text.Json;

namespace ST.Tests.Unit.Jobs;

public sealed class MediatorSerializedJobTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Execute_ForwardsPayloadAndCancellation_AndDisposesScope(bool handlerFails)
    {
        var sender = Substitute.For<ISender>();
        AsyncScopeProbe? probe = null;
        var services = new ServiceCollection();
        var tenantId = Guid.CreateVersion7();
        var tenantOverride = Substitute.For<IWorkspaceTenantOverride>();
        var plane = Substitute.For<IControlPlaneUnitOfWork>();
        var tenants = Substitute.For<ITenantRepository>();
        plane.Tenants.Returns(tenants);
        tenants.AnyAsync(Arg.Any<Expression<Func<Tenant, bool>>>(), Arg.Any<CancellationToken>()).Returns(true);
        services.AddSingleton(plane);
        services.AddSingleton(tenantOverride);
        services.AddScoped<AsyncScopeProbe>();
        services.AddScoped<ISender>(provider =>
        {
            probe = provider.GetRequiredService<AsyncScopeProbe>();
            return sender;
        });
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var cancellation = new CancellationTokenSource();
        var command = new TestCommand { Value = "scheduled command" };
        var context = Substitute.For<IJobExecutionContext>();
        context.MergedJobDataMap.Returns(new JobDataMap
        {
            ["TenantId"] = tenantId.ToString(),
            ["CommandData"] = JsonSerializer.Serialize(command),
            ["CommandType"] = typeof(TestCommand).AssemblyQualifiedName!
        });
        if (handlerFails)
        {
            sender.Send(Arg.Any<IRequest>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException(new InvalidOperationException("Handler failed")));
        }
        var job = new MediatorSerializedJob(provider, new BackgroundJobSettings());

        if (handlerFails)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute(context, cancellation.Token).AsTask());
        }
        else
        {
            await job.Execute(context, cancellation.Token);
        }

        await sender.Received(1).Send(
            Arg.Is<IRequest>(request => request is TestCommand && ((TestCommand)request).Value == command.Value),
            cancellation.Token);
        Assert.NotNull(probe);
        Assert.True(probe.Disposed);
        Assert.Equal(tenantId, tenantOverride.TenantId);
    }

    [Fact]
    public async Task Execute_WhenAlreadyCancelled_DoesNotReadPayload()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var context = Substitute.For<IJobExecutionContext>();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var job = new MediatorSerializedJob(provider, new BackgroundJobSettings());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => job.Execute(context, cancellation.Token).AsTask());

        _ = context.DidNotReceive().MergedJobDataMap;
    }

    private sealed class TestCommand : IRequest
    {
        public string Value { get; init; } = string.Empty;
    }

    private sealed class AsyncScopeProbe : IAsyncDisposable
    {
        public bool Disposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
