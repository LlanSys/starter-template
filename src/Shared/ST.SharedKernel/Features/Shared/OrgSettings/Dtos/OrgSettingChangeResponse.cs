namespace ST.SharedKernel.Features.Shared.OrgSettings.Dtos;
public sealed record OrgSettingChangeResponse(string Key, string? PreviousValue, string? Value, bool Reset, string ActorId, DateTimeOffset At);
