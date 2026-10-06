
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ST.Application.Utilities;

/// <summary>
/// Single source of truth for every cache key pattern in the application.
///
/// Key anatomy:
///   Entity (non-versioned):  "{group}:entity:{discriminator}"
///   List   (versioned):      "{group}:list:{scope}:{versionToken}:{discriminator}"
///   Version sentinel:        "{group}:version:{scope}"
///
/// Where {scope} is either the userId or the literal "global".
///
/// Rules:
///   - Only this class builds keys. Behaviors and queries call helpers here.
///   - Hashing is stable (SHA-256, hex-encoded, trimmed to 16 chars).
///   - All keys are lowercase to prevent accidental duplicates.
/// </summary>
public static class CacheKeys
{
    // ── Public key builders ────────────────────────────────────────────────────

    /// <summary>
    /// Version sentinel key for a cache group.
    /// Bumping this key orphans every versioned list entry in the group.
    /// </summary>
    /// <param name="group">e.g. "customers"</param>
    /// <param name="userId">Null for a global (cross-user) version token.</param>
    public static string GroupVersion(string group, string? userId = null)
        => userId is null
            ? $"{NormalizeRequired(group, nameof(group))}:version:global"
            : $"{NormalizeRequired(group, nameof(group))}:version:{NormalizeRequired(userId, nameof(userId))}";

    /// <summary>
    /// Non-versioned key for a single entity lookup.
    /// Invalidated directly by its exact key when the entity is mutated.
    /// </summary>
    /// <param name="group">e.g. "customers"</param>
    /// <param name="id">String form of the entity identifier.</param>
    public static string Entity(string group, string id)
        => $"{NormalizeRequired(group, nameof(group))}:entity:{NormalizeRequired(id, nameof(id))}";

    /// <summary>
    /// Non-versioned key for email template content.
    /// </summary>
    /// <param name="templateName">Logical email template name.</param>
    public static string EmailTemplate(string templateName)
        => $"email-templates:entity:{NormalizeRequired(templateName, nameof(templateName))}";

    public static string TotpAttempts(string userId)
        => $"auth:totp-attempts:{NormalizeRequired(userId, nameof(userId))}";

    public static string PasswordResetCode(string userId)
        => $"auth:password-reset-code:{NormalizeRequired(userId, nameof(userId))}";

    public static string PasswordResetAttempts(string userId)
        => $"auth:password-reset-attempts:{NormalizeRequired(userId, nameof(userId))}";

    public static string PasswordReset(string userId)
        => $"auth:password-reset:{NormalizeRequired(userId, nameof(userId))}";



    public static string EmailOtp(string userId)
        => $"otp:email:{NormalizeRequired(userId, nameof(userId))}";

    public static string EmailOtpCooldown(string userId)
        => $"otp:email:cooldown:{NormalizeRequired(userId, nameof(userId))}";

    public static string EmailOtpAttempts(string userId)
        => $"otp:email:attempts:{NormalizeRequired(userId, nameof(userId))}";

    public static string PasswordResetOtp(string userId)
        => $"pwdreset:otp:{NormalizeRequired(userId, nameof(userId))}";

    public static string PasswordResetRateLimit(string userId)
        => $"pwdreset:ratelimit:{NormalizeRequired(userId, nameof(userId))}";

    /// <summary>
    /// Assembles the full versioned list key.
    /// Called by <see cref="Behaviours.CachingBehavior{TRequest,TResponse}"/>
    /// after it has resolved the version token — not by queries directly.
    /// </summary>
    internal static string VersionedList(
        string group,
        string scope,
        string versionToken,
        string discriminator)
        => $"{NormalizeRequired(group, nameof(group))}:list:{NormalizeRequired(scope, nameof(scope))}:{NormalizeRequired(versionToken, nameof(versionToken))}:{NormalizeRequired(discriminator, nameof(discriminator))}";


    // ── Discriminator helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Produces a stable, compact discriminator for any filter object.
    /// Serialises the object properties in a deterministic order using
    /// <paramref name="raw"/> (the caller composes the canonical string).
    ///
    /// Returns the first 16 hex characters of the SHA-256 hash — enough
    /// uniqueness for any realistic filter space (2^64 values).
    /// </summary>
    public static string HashFilter(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    // ── Named discriminator builders (one per filterable entity) ──────────────


    public static string Discriminator<T>(T filter) where T : class
    {
        var json = JsonSerializer.Serialize(filter);
        return HashFilter(json);
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Cache key segment cannot be null or whitespace.", paramName)
            : value.Trim().ToLowerInvariant();
    }


}
