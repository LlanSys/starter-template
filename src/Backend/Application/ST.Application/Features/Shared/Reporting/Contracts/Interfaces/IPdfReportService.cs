using ST.SharedKernel.Features.Shared.Reporting.Dtos;

namespace ST.Application.Features.Shared.Reporting.Contracts.Interfaces;

public interface IPdfReportService
{
    PdfReportResponse Generate(PdfReportRequest request);
}
