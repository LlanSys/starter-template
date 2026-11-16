using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ST.Application.Features.ControlPlane.Kyc.Contracts;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Features.ControlPlane.Kyc;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
namespace ST.Tests.Integration.TestFixtures;

public sealed class KycScannerFixture : IAsyncLifetime
{
    private readonly IContainer _container = new ContainerBuilder().WithImage("clamav/clamav:1.5.4")
        .WithEnvironment("TZ", "UTC")
        .WithEnvironment("CLAMAV_NO_FRESHCLAMD", "true")
        .WithEnvironment("CLAMD_CONF_AlertEncrypted", "true")
        .WithEnvironment("CLAMD_CONF_AlertExceedsMax", "true")
        .WithEnvironment("CLAMD_CONF_ConcurrentDatabaseReload", "false")
        .WithEnvironment("CLAMD_CONF_MaxThreads", "2")
        .WithPortBinding(3310, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted(new[] { "clamdscan", "--ping", "1" },
            strategy => strategy.WithTimeout(TimeSpan.FromMinutes(5))))
        .Build();
    public string Host => _container.Hostname;
    public int Port => _container.GetMappedPublicPort(3310);
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var update = await _container.ExecAsync(new[] { "freshclam", "--stdout" }, deadline.Token);
        Assert.True(update.ExitCode == 0, "The synthetic scanner fixture requires current antivirus definitions.");
        var reload = await _container.ExecAsync(new[] { "clamdscan", "--reload" }, deadline.Token);
        Assert.Equal(0, reload.ExitCode);
        var verifier = new ClamAvKycDocumentScanner(Options.Create(new KycDocumentSafetySettings { Host = Host, Port = Port }),
            Options.Create(new KycDocumentStorageSettings()), TimeProvider.System, NullLogger<ClamAvKycDocumentScanner>.Instance);

        while (true)
        {
            using var content = new MemoryStream(new byte[] { 1, 2, 3 });
            var result = await verifier.ScanAsync(content, deadline.Token);

            if (result.Verdict == KycScanVerdict.Clean)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), deadline.Token);
        }
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
