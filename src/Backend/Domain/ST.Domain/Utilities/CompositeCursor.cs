using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Utilities;

/// <summary>
/// Used when cursor pagination requires two fields for stable ordering,
/// e.g. (CreatedAt, Id) for non-sequential IDs.
/// Serialised as "timestamp|guid" — opaque to the client.
/// For UUIDv7 Guid-only cursors, this is not needed.
/// </summary>
public record CompositeCursor(DateTimeOffset CreatedAt, Guid Id)
{
    public override string ToString() => $"{CreatedAt.ToUnixTimeMilliseconds()}|{Id}";

    public static CompositeCursor? Parse(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;

        var parts = value.Split('|');

        if (parts.Length != 2) return null;

        if (!long.TryParse(parts[0], out var ms)) return null;
        if (!Guid.TryParse(parts[1], out var id)) return null;

        return new CompositeCursor(DateTimeOffset.FromUnixTimeMilliseconds(ms), id);

    }
}
