using System.Globalization;
using ST.SharedKernel.Features.Shared.Dashboard;

namespace ST.UI.Blazor.Features.Shared.WorkTasks;

internal static class TaskApiRoutes
{
    public static string ForTask(DashboardTaskKey key, string operation)
    {
        return $"api/v1/dashboard/tasks/{Uri.EscapeDataString(key.Kind)}/{operation}?subjectId={key.SubjectId:D}"
            + $"&cycleId={key.CycleId:D}&cycleAt={Uri.EscapeDataString(key.CycleAt.ToString("O", CultureInfo.InvariantCulture))}";
    }
}
