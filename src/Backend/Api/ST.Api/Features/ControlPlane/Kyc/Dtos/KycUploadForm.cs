using System.ComponentModel.DataAnnotations;

namespace ST.Api.Features.ControlPlane.Kyc.Dtos;

public sealed class KycUploadForm
{
    [Required]
    [StringLength(80)]
    public string DocumentType { get; set; } = string.Empty;

    [StringLength(80)]
    public string? OtherDocumentName { get; set; }

    [Required]
    public IFormFile File { get; set; } = null!;

    [StringLength(64)]
    public string? Token { get; set; }
}
