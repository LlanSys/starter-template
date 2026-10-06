using System;
using System.ComponentModel.DataAnnotations;

namespace ST.SharedKernel.Features.Shared.OrgSettings.Dtos;

public record UpdateOrgSettingRequest(
    [Required] Guid Id,
    [Required] string Key,
    [Required] string Value,
    string? Description);
