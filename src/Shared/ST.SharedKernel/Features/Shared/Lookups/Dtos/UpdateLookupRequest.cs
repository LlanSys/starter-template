using System.ComponentModel.DataAnnotations;

namespace ST.SharedKernel.Features.Shared.Lookups.Dtos;

public sealed record UpdateLookupRequest(
    [Required, StringLength(100)] string Code,
    [Required, StringLength(200)] string Description);
