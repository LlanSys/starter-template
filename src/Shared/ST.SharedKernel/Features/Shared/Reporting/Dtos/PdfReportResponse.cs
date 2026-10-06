namespace ST.SharedKernel.Features.Shared.Reporting.Dtos;

public sealed record PdfReportResponse(
    byte[] Content,
    string ContentType,
    string FileName);
