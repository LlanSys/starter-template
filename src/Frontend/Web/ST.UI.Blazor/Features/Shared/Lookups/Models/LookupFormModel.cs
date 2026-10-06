using ST.SharedKernel.Features.Shared.Lookups.Dtos;

namespace ST.UI.Blazor.Features.Shared.Lookups.Models;

internal sealed class LookupFormModel
{
    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public static LookupFormModel From(LookupResponse lookup)
    {
        ArgumentNullException.ThrowIfNull(lookup);

        return new LookupFormModel
        {
            Code = lookup.Code,
            Description = lookup.Description ?? string.Empty
        };
    }

    public CreateLookupRequest ToCreateRequest() => new(Code, Description);

    public UpdateLookupRequest ToUpdateRequest() => new(Code, Description);
}
