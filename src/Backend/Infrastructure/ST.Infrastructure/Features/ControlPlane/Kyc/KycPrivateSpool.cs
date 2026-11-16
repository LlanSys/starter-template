using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace ST.Infrastructure.Features.ControlPlane.Kyc;

internal static class KycPrivateSpool
{
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "On success the verified stream ownership transfers to the caller; failures dispose it in finally.")]
    public static async Task<FileStream?> VerifyAsync(Stream source, long length, string sha256,
        long maximumLength, CancellationToken cancellationToken)
    {
        if (length <= 0 || length > maximumLength) return null;
        var options = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.ReadWrite,
            Share = FileShare.None,
            Options = FileOptions.Asynchronous | FileOptions.DeleteOnClose,
            BufferSize = 81920
        };

        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        FileStream? spool = null;
        var buffer = ArrayPool<byte>.Shared.Rent(81920);

        try
        {
            spool = new FileStream(Path.Combine(Path.GetTempPath(), $"st-kyc-safety-{Guid.CreateVersion7():N}"), options);

            long total = 0;
            int read;

            while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                total += read;

                if (total > length || total > maximumLength) return null;
                await spool.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }

            if (total != length) return null;
            spool.Position = 0;
            var actual = Convert.ToHexString(await SHA256.HashDataAsync(spool, cancellationToken).ConfigureAwait(false));

            if (!string.Equals(actual, sha256, StringComparison.OrdinalIgnoreCase)) return null;
            spool.Position = 0;
            var result = spool;
            spool = null;
            return result;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, true);

            if (spool is not null) await spool.DisposeAsync().ConfigureAwait(false);
        }
    }
}
