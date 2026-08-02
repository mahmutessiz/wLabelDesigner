using System.Text;
using wLabelDesigner.Models;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class PdfExportTests
{
    [Fact]
    public void Render_CreatesExactSizeSinglePagePdfWithValidCrossReference()
    {
        var document = new LabelDocument
        {
            WidthMillimeters = 25.4,
            HeightMillimeters = 12.7,
            PrinterDpi = 203,
            Elements =
            [
                new LabelElementData
                {
                    Kind = LabelElementKind.Text,
                    Content = "PDF",
                    Width = 20,
                    Height = 8
                }
            ]
        };

        var pdf = LabelPdfRenderer.Render(document);
        var text = Encoding.ASCII.GetString(pdf);

        Assert.StartsWith("%PDF-1.4\n", text);
        Assert.Contains("/Count 1", text);
        Assert.Contains("/MediaBox [0 0 72 36]", text);
        Assert.Contains("/Subtype /Image /Width 203 /Height 102", text);
        Assert.EndsWith("%%EOF\n", text);

        var markerIndex = text.LastIndexOf("startxref\n", StringComparison.Ordinal);
        Assert.True(markerIndex >= 0);
        var offsetStart = markerIndex + "startxref\n".Length;
        var offsetEnd = text.IndexOf('\n', offsetStart);
        var crossReferenceOffset = int.Parse(text[offsetStart..offsetEnd]);
        Assert.Equal("xref\n", Encoding.ASCII.GetString(pdf, crossReferenceOffset, 5));
    }

    [Fact]
    public async Task ExportPdfCommand_ReportsExportedFile()
    {
        var exportService = new StubExportService("C:\\Exports\\shipping-label.pdf");
        var viewModel = new MainViewModel(
            new StubDocumentStore(),
            new StubFileDialogService(),
            new StubPrintService(),
            new StubClipboard(),
            labelExportService: exportService);

        await viewModel.ExportPdfCommand.ExecuteAsync(null);

        Assert.NotNull(exportService.Document);
        Assert.Equal("PDF exported to shipping-label.pdf", viewModel.StatusMessage);
    }

    private sealed class StubExportService(string? result) : ILabelExportService
    {
        public LabelDocument? Document { get; private set; }

        public Task<string?> ExportPngAsync(
            LabelDocument document,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task<string?> ExportPdfAsync(
            LabelDocument document,
            CancellationToken cancellationToken = default)
        {
            Document = document;
            return Task.FromResult(result);
        }
    }

    private sealed class StubDocumentStore : ILabelDocumentStore
    {
        public Task<LabelDocument> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(new LabelDocument());

        public Task SaveAsync(string path, LabelDocument document, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubFileDialogService : IFileDialogService
    {
        public string? ChooseTemplateToOpen() => null;

        public string? ChooseTemplateToSave(string suggestedFileName) => null;
    }

    private sealed class StubPrintService : ILabelPrintService
    {
        public bool Print(LabelDocument document) => false;
    }

    private sealed class StubClipboard : IElementClipboard
    {
        public bool ContainsElement() => false;

        public bool TryCopy(LabelElementData element) => false;

        public bool TryGetElement(out LabelElementData? element)
        {
            element = null;
            return false;
        }
    }
}
