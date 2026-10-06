namespace ST.SharedKernel.Features.Shared.Reporting.Dtos;

public sealed record PdfReportRequest(
    string Title,
    IReadOnlyCollection<PdfReportSection> Sections);
