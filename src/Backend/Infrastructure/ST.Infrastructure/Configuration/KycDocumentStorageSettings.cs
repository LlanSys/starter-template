using System.ComponentModel.DataAnnotations;
using ST.SharedKernel.Features.ControlPlane.Kyc;
using ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

namespace ST.Infrastructure.Configuration;

public sealed class KycDocumentStorageSettings
{
    public const string SectionName = "KycDocuments";

    public KycDocumentTypeOption[] DocumentTypes { get; init; } = [];

    public bool HasValidDocumentTypes()
    {
        return DocumentTypes is { Length: >= 1 and <= 30 }
            && DocumentTypes.All(item => item is not null && !string.IsNullOrWhiteSpace(item.Key)
                && item.Key.Length <= 64 && item.Key.All(character => char.IsAsciiLetterLower(character)
                    || char.IsAsciiDigit(character) || character == '-')
                && item.Key != KycDocumentTypes.Other && item.Key != KycDocumentTypes.Legacy
                && !string.IsNullOrWhiteSpace(item.Name) && item.Name.Trim().Length <= 80
                && !item.Name.Any(char.IsControl) && item.Guidance is not null && item.Guidance.Length <= 240)
            && DocumentTypes.Select(item => item.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() == DocumentTypes.Length;
    }

    public string Provider { get; init; } = "Azurite";
    public string? ConnectionString { get; init; }
    public string? ContainerUri { get; init; }
    public string ErasureLedgerContainerName { get; set; } = "kyc-erasure-ledger";
    public string ContainerName { get; init; } = "kyc-documents";

    [Range(1, 10_485_760)]
    public long MaxFileBytes { get; init; } = 10_485_760;

    [Range(1, 168)]
    public int SubmissionLinkHours { get; init; } = 48;

    [Range(1, 100)]
    public int SubmissionUploadLimit { get; init; } = 20;
}
