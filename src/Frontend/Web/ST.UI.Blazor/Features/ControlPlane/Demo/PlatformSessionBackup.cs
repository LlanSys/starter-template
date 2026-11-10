using ST.UI.Blazor.Features.IAM.Users.Models;

namespace ST.UI.Blazor.Features.ControlPlane.Demo;

public sealed record PlatformSessionBackup(
    string? AccessToken,
    string? RefreshToken,
    string? SessionId,
    WorkspaceSelectionInfo? Workspace = null);
