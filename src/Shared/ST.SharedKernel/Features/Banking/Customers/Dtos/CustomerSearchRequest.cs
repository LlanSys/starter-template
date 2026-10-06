using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ST.SharedKernel.Features.Banking.Customers.Dtos;

/// <summary>
/// Search and filter parameters for the customer listing.
/// All filter fields are optional — the spec only applies a filter when the field has a value.
/// Uses cursor-based pagination — no page number, just the last seen ID.
/// </summary>
public record CustomerSearchRequest(

    // ── Free-text global search ─────────────────────────────────────────────
    /// <summary>
    /// Searches across Number, CompanyName, RegistrationNumber,
    /// TINNumber, Mobile, and EmailId simultaneously.
    /// </summary>
    string? GlobalSearch = null,

    // ── Discrete field filters — all optional ───────────────────────────────
    string? Type = null,
    string? SegmentType = null,
    string? SubSegmentType = null,
    string? IdentificationType = null,
    string? LineOfBusiness = null,
    string? Status = null,
    Guid? RelationshipManagerId = null,
    Guid? Cursor = null,
    int PageSize = 50
)
{
    public string BuildQueryString()
    {
        var parameters = new List<string>(9);
        Add(parameters, nameof(GlobalSearch), GlobalSearch);
        Add(parameters, nameof(Type), Type);
        Add(parameters, nameof(SegmentType), SegmentType);
        Add(parameters, nameof(SubSegmentType), SubSegmentType);
        Add(parameters, nameof(IdentificationType), IdentificationType);
        Add(parameters, nameof(LineOfBusiness), LineOfBusiness);
        Add(parameters, nameof(Status), Status);
        Add(parameters, nameof(RelationshipManagerId), RelationshipManagerId?.ToString());
        Add(parameters, nameof(Cursor), Cursor?.ToString());
        Add(parameters, nameof(PageSize), PageSize.ToString(CultureInfo.InvariantCulture));

        return parameters.Count == 0 ? string.Empty : $"?{string.Join('&', parameters)}";
    }

    private static void Add(List<string> parameters, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        parameters.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
    }


}