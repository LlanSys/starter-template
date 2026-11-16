using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ST.Application.Features.ControlPlane.Kyc.Contracts;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Features.ControlPlane.Kyc;

namespace ST.Tests.Unit.Infrastructure;

public sealed class KycScannerProtocolTests
{
    [Theory]
    [InlineData("stream: OK", KycScanVerdict.Clean)]
    [InlineData("stream: Win.Test.EICAR_HDB-1 FOUND", KycScanVerdict.Unsafe)]
    [InlineData("stream: Heuristics.Encrypted.PDF FOUND", KycScanVerdict.Blocked)]
    [InlineData("stream: Heuristics.Limits.Exceeded FOUND", KycScanVerdict.Blocked)]
    [InlineData("stream: UNKNOWN ERROR", KycScanVerdict.Unavailable)]
    public async Task ProtocolOnlyClearsAnExplicitCleanResult(string response, KycScanVerdict expected)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var version = $"ClamAV 1.5.4/12345/{TimeProvider.System.GetUtcNow().ToString("ddd MMM d HH:mm:ss yyyy", CultureInfo.InvariantCulture)}";
        var bytes = Encoding.UTF8.GetBytes("synthetic evidence");
        var server = ServeAsync(listener, version, response, version, bytes);
        var scanner = Scanner(((IPEndPoint)listener.LocalEndpoint).Port);
        using var content = new MemoryStream(bytes);
        Assert.Equal(expected, (await scanner.ScanAsync(content, default)).Verdict);
        await server;
    }

    [Theory]
    [InlineData("malformed", false)]
    [InlineData("ClamAV 1.5.4/1/Sat Jan 1 00:00:00 2000", false)]
    [InlineData("", true)]
    public async Task MissingStaleOrTruncatedDefinitionsNeverClearEvidence(string version, bool truncate)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var server = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();
            await ReadCommandAsync(stream);
            await stream.WriteAsync(Encoding.ASCII.GetBytes(version + (truncate ? "" : "\0")));
        });

        using var content = new MemoryStream([1, 2, 3]);
        Assert.Equal(KycScanVerdict.Unavailable, (await Scanner(((IPEndPoint)listener.LocalEndpoint).Port).ScanAsync(content, default)).Verdict);
        await server;
    }

    [Fact]
    public async Task ChangedDefinitionsInvalidateAnOtherwiseCleanResult()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var date = TimeProvider.System.GetUtcNow().ToString("ddd MMM d HH:mm:ss yyyy", CultureInfo.InvariantCulture);
        byte[] bytes = [1, 2, 3];
        var server = ServeAsync(listener, $"ClamAV 1.5.4/123/{date}", "stream: OK", $"ClamAV 1.5.4/124/{date}", bytes);
        using var content = new MemoryStream(bytes);
        var result = await Scanner(((IPEndPoint)listener.LocalEndpoint).Port).ScanAsync(content, default);
        Assert.Equal(KycScanVerdict.Unavailable, result.Verdict);
        Assert.Equal("definitions-changed", result.ReasonCode);
        await server;
    }

    [Fact]
    public async Task IntegritySpoolRejectsChangedOrOversizedStorageContent()
    {
        using var changed = new MemoryStream([1, 2, 3]);
        Assert.Null(await KycPrivateSpool.VerifyAsync(changed, 3, new string('A', 64), 10, default));
        using var oversized = new MemoryStream([1, 2, 3, 4]);
        Assert.Null(await KycPrivateSpool.VerifyAsync(oversized, 3, new string('A', 64), 10, default));
    }

    private static ClamAvKycDocumentScanner Scanner(int port)
    {
        return new(Options.Create(new KycDocumentSafetySettings { Host = "127.0.0.1", Port = port }),
            Options.Create(new KycDocumentStorageSettings()), TimeProvider.System, NullLogger<ClamAvKycDocumentScanner>.Instance);
    }

    private static async Task ServeAsync(TcpListener listener, string version, string response, string after, byte[] expected)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        foreach (var reply in new[] { version, response, after })
        {
            using var client = await listener.AcceptTcpClientAsync(deadline.Token);
            await using var stream = client.GetStream();
            var command = await ReadCommandAsync(stream);

            if (command == "zINSTREAM")
            {
                using var received = new MemoryStream();
                var header = new byte[4];

                while (true)
                {
                    await stream.ReadExactlyAsync(header, deadline.Token);
                    var length = BinaryPrimitives.ReadInt32BigEndian(header);

                    if (length == 0)
                    {
                        break;
                    }

                    Assert.InRange(length, 1, 65536);
                    var buffer = new byte[length];
                    await stream.ReadExactlyAsync(buffer, deadline.Token);
                    await received.WriteAsync(buffer, deadline.Token);
                }
                Assert.Equal(expected, received.ToArray());
            }
            else
            {
                Assert.Equal("zVERSION", command);
            }

            await stream.WriteAsync(Encoding.ASCII.GetBytes(reply + "\0"), deadline.Token);
        }
    }

    private static async Task<string> ReadCommandAsync(Stream stream)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var bytes = new List<byte>();
        var one = new byte[1];

        while (true)
        {
            await stream.ReadExactlyAsync(one, deadline.Token);

            if (one[0] == 0)
            {
                return Encoding.ASCII.GetString(bytes.ToArray());
            }

            bytes.Add(one[0]);
        }
    }
}
