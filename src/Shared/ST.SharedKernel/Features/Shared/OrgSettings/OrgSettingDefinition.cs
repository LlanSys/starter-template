namespace ST.SharedKernel.Features.Shared.OrgSettings;
public sealed record OrgSettingDefinition(string Key, string Label, string Description, string DefaultValue,
    IReadOnlyList<OrgSettingOption> Options);
