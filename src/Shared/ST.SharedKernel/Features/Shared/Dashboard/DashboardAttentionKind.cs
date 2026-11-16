using System.Text.Json.Serialization;

namespace ST.SharedKernel.Features.Shared.Dashboard;

[JsonConverter(typeof(JsonStringEnumConverter<DashboardAttentionKind>))]
public enum DashboardAttentionKind
{
    PersonalAction,
    TeamAction,
    Information,
    AssignedAction
}
