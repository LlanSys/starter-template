using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Security.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ST.Application.Features.ControlPlane.Kyc.Contracts;
using ST.Infrastructure.Configuration;

namespace ST.Infrastructure.Features.ControlPlane.Kyc;

internal sealed partial class ClamAvKycDocumentScanner(
    IOptions<KycDocumentSafetySettings> options,
    IOptions<KycDocumentStorageSettings> storageOptions,
    TimeProvider clock,
    ILogger<ClamAvKycDocumentScanner> logger) : IKycDocumentScanner
{
    public async Task<KycScanResult> ScanAsync(Stream content, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(settings.TimeoutSeconds), clock);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);

        try
        {
            var version = await CommandAsync("VERSION", null, linked.Token).ConfigureAwait(false);
            var parts = version.Split('/');
            var now = clock.GetUtcNow();

            if (parts.Length != 3 || !parts[0].StartsWith("ClamAV ", StringComparison.Ordinal)
                || parts[0].Length <= 7 || parts[0].Length > 128 || parts[1].Length > 64
                || !ulong.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out _)
                || !DateTimeOffset.TryParseExact(parts[2].Replace("  ", " ", StringComparison.Ordinal), "ddd MMM d HH:mm:ss yyyy", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var definitionsAt)
                || definitionsAt > now.AddMinutes(5)
                || now - definitionsAt > TimeSpan.FromHours(settings.MaxDefinitionsAgeHours))
            {
                return new(KycScanVerdict.Unavailable, "ClamAV", "", null, "definitions-unavailable");
            }

            var response = await CommandAsync("INSTREAM", content, linked.Token).ConfigureAwait(false);

            if (!string.Equals(version, await CommandAsync("VERSION", null, linked.Token).ConfigureAwait(false), StringComparison.Ordinal))
            {
                return new(KycScanVerdict.Unavailable, parts[0], parts[1], definitionsAt, "definitions-changed");
            }

            var verdict = response switch
            {
                "stream: OK" => KycScanVerdict.Clean,
                _ when response.EndsWith(" FOUND", StringComparison.Ordinal)
                    && (response.Contains("Encrypted", StringComparison.OrdinalIgnoreCase)
                        || response.Contains("Limits.Exceeded", StringComparison.OrdinalIgnoreCase)) => KycScanVerdict.Blocked,
                _ when response.EndsWith(" FOUND", StringComparison.Ordinal) => KycScanVerdict.Unsafe,
                _ when response.Contains("size limit exceeded", StringComparison.OrdinalIgnoreCase) => KycScanVerdict.Blocked,
                _ => KycScanVerdict.Unavailable
            };

            return new(verdict, parts[0], parts[1], definitionsAt, verdict switch
            {
                KycScanVerdict.Clean => "clean",
                KycScanVerdict.Unsafe => "unsafe-content",
                KycScanVerdict.Blocked => "unscannable-content",
                _ => "scanner-unavailable"
            });
        }
        catch (Exception exception) when (exception is IOException or SocketException or OperationCanceledException or AuthenticationException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            LogUnavailable(logger, exception);
            return new(KycScanVerdict.Unavailable, "ClamAV", "", null, "scanner-unavailable");
        }
    }

    private async Task<string> CommandAsync(string command, Stream? content, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var client = new TcpClient();
        await client.ConnectAsync(settings.Host, settings.Port, cancellationToken).ConfigureAwait(false);
        await using var network = client.GetStream();

        if (settings.UseTls)
        {
            await using var tls = new SslStream(network, leaveInnerStreamOpen: true);
            await tls.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = settings.Host }, cancellationToken).ConfigureAwait(false);
            return await ExchangeAsync(tls, command, content, cancellationToken).ConfigureAwait(false);
        }
        return await ExchangeAsync(network, command, content, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> ExchangeAsync(Stream connection, string command, Stream? content, CancellationToken cancellationToken)
    {
        await connection.WriteAsync(Encoding.ASCII.GetBytes($"z{command}\0"), cancellationToken).ConfigureAwait(false);

        if (content is not null)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(65536);

            try
            {
                var header = new byte[4];
                long total = 0;
                int read;

                while ((read = await content.ReadAsync(buffer.AsMemory(0, 65536), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    total += read;

                    if (total > storageOptions.Value.MaxFileBytes)
                    {
                        throw new IOException("Scan stream exceeded its configured bound.");
                    }

                    BinaryPrimitives.WriteInt32BigEndian(header, read);
                    await connection.WriteAsync(header, cancellationToken).ConfigureAwait(false);
                    await connection.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                }
                await connection.WriteAsync(new byte[4], cancellationToken).ConfigureAwait(false);
            }
            finally { ArrayPool<byte>.Shared.Return(buffer, true); }
        }
        var response = new byte[1024];
        var offset = 0;

        while (offset < response.Length)
        {
            var read = await connection.ReadAsync(response.AsMemory(offset, 1), cancellationToken).ConfigureAwait(false);

            if (read == 0)
            {
                throw new IOException("Incomplete scanner response.");
            }

            if (response[offset] == 0)
            {
                return Encoding.ASCII.GetString(response, 0, offset).Trim();
            }

            offset++;
        }
        throw new IOException("Scanner response exceeded its configured bound.");
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "KYC document scanner is unavailable.")]
    private static partial void LogUnavailable(ILogger logger, Exception exception);
}
