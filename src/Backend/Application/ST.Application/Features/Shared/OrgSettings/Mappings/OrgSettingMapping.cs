using ST.Domain.Features.Shared.OrgSettings.Entities;
using ST.SharedKernel.Features.Shared.OrgSettings.Dtos;
using System.Diagnostics.CodeAnalysis;

namespace ST.Application.Features.Shared.OrgSettings.Mappings;

public static class OrgSettingMapping
{
    public static OrgSettingResponse ToResponse(this OrgSetting entity)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        return new OrgSettingResponse(
            entity.Id,
            entity.Key,
            IsSensitiveKey(entity.Key) ? "***" : entity.Value,
            entity.Description,
            entity.CreatedAt.UtcDateTime,
            entity.CreatedBy,
            entity.UpdatedAt?.UtcDateTime,
            entity.UpdatedBy
        );
    }

    public static bool IsSensitiveKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        var lowerKey = key.ToLowerInvariant();
        return lowerKey.Contains("secret") ||
               lowerKey.Contains("password") ||
               lowerKey.Contains("token") ||
               lowerKey.Contains("key");
    }
}
