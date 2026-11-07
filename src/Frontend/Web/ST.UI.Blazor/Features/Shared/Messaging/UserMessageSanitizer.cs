using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ST.UI.Blazor.Features.Shared.Messaging;

internal static class UserMessageSanitizer
{
    public static bool IsDevelopment { get; set; }

    private static readonly Regex GuidRegex = new(
        @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static readonly string[] SensitiveFragments =
    [
        "System.",
        "Exception",
        "StackTrace",
        " at ",
        "Microsoft.",
        "SqlException",
        "DbUpdateException",
        "RequestFailedException",
        "CryptographicException",
        "AuthenticationException",
        "InvalidOperationException",
        "NullReferenceException",
        "ArgumentNullException",
        "AuthorizationPermissionMismatch",
        "key ring",
        "keyring",
        "dataprotection",
        "unprotect",
        "protect",
        "decrypt",
        "encrypt",
        "aka.ms",
        "license",
        "licensing",
        "mediatr",
        "masstransit",
        "fluentvalidation",
        "stack trace",
        "stacktrace",
        "hresult",
        "error code",
        "invalid operation",
        "database",
        "connection",
        "socket",
        "network"
    ];

    public static string Normalize(string? message, string fallback)
    {
        if (string.IsNullOrWhiteSpace(message)
            || string.Equals(message, "Operation Successful", StringComparison.OrdinalIgnoreCase))
        {
            return fallback;
        }

        if (LooksTechnical(message))
        {
            return fallback;
        }

        return message.Trim();
    }

    public static string? NormalizeNullable(string? message, string fallback)
        => string.IsNullOrWhiteSpace(message) ? null : Normalize(message, fallback);

    private static bool LooksTechnical(string message)
        => SensitiveFragments.Any(fragment => message.Contains(fragment, StringComparison.OrdinalIgnoreCase))
           || ContainsGuidPattern(message)
           || message.Contains("https://", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsGuidPattern(string message)
        => !string.IsNullOrWhiteSpace(message) && GuidRegex.IsMatch(message);
}
