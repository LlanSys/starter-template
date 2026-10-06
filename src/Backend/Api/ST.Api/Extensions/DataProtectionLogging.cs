using Microsoft.Extensions.Logging;

namespace ST.Api.Extensions;

internal static class DataProtectionLogging
{
    private static readonly Action<ILogger, string, Exception?> _certNotFound =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(2001, nameof(CertificateNotFound)),
            "Data protection certificate with thumbprint {Thumbprint} not found in CurrentUser\\My store.");

    private static readonly Action<ILogger, string, Exception?> _certNoPrivateKey =
        LoggerMessage.Define<string>(LogLevel.Warning, new EventId(2002, nameof(CertificateNoPrivateKey)),
            "Certificate {Thumbprint} does not contain a private key. Upload the PFX (not a .cer).");

    private static readonly Action<ILogger, string, Exception?> _certLoaded =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(2003, nameof(CertificateLoaded)),
            "Data protection certificate with thumbprint {Thumbprint} loaded and will be used to protect keys.");

    public static void CertificateNotFound(ILogger logger, string thumbprint) => _certNotFound(logger, thumbprint, null);
    public static void CertificateNoPrivateKey(ILogger logger, string thumbprint) => _certNoPrivateKey(logger, thumbprint, null);
    public static void CertificateLoaded(ILogger logger, string thumbprint) => _certLoaded(logger, thumbprint, null);
}
