namespace ST.SharedKernel.Features.Shared.OrgSettings.Dtos;
public sealed record CatalogOrgSettingResponse(OrgSettingDefinition Definition, string Value, bool IsOverridden, string? Revision);
