using Microsoft.AspNetCore.Components.Forms;

namespace ST.UI.Blazor.Features.ControlPlane.Tenants.Components;

public sealed class KycDocumentUploadRow(string typeKey, bool additional = false)
{
    public Guid Id { get; } = Guid.CreateVersion7();
    public Guid InputKey { get; set; } = Guid.CreateVersion7();
    public string TypeKey { get; set; } = typeKey;
    public bool Additional { get; } = additional;
    public string OtherName { get; set; } = string.Empty;
    public IBrowserFile? File { get; set; }
}
