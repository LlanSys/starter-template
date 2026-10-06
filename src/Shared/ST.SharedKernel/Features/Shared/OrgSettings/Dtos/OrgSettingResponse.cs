using System;
using System.ComponentModel.DataAnnotations;

namespace ST.SharedKernel.Features.Shared.OrgSettings.Dtos;

public record OrgSettingResponse(
    [Required] Guid Id,
    [Required] string Key,
    [Required] string Value,
    string? Description,
    DateTime CreatedAt,
    string CreatedBy,
    DateTime? UpdatedAt,
    string? UpdatedBy);
