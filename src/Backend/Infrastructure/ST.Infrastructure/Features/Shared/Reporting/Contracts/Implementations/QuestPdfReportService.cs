using ST.Application.Features.Shared.Reporting.Contracts.Interfaces;
using ST.SharedKernel.Features.Shared.Reporting.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ST.Infrastructure.Features.Shared.Reporting.Contracts.Implementations;

internal sealed class QuestPdfReportService : IPdfReportService
{
    public PdfReportResponse Generate(PdfReportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Title);
        ArgumentNullException.ThrowIfNull(request.Sections);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(text => text.FontSize(10));

                page.Header()
                    .Text(request.Title)
                    .SemiBold()
                    .FontSize(18)
                    .FontColor(Colors.Green.Darken4);

                page.Content()
                    .PaddingVertical(24)
                    .Column(column =>
                    {
                        foreach (var section in request.Sections)
                        {
                            column.Item().Text(section.Heading).SemiBold().FontSize(12);
                            column.Item().PaddingBottom(12).Text(section.Body);
                        }
                    });

                page.Footer()
                    .AlignRight()
                    .Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
            });
        });

        var content = document.GeneratePdf();
        var safeFileName = request.Title
            .Replace(" ", "-", StringComparison.Ordinal)
            .ToLowerInvariant();

        return new PdfReportResponse(content, "application/pdf", $"{safeFileName}.pdf");
    }
}
