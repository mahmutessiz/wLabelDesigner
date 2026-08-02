using System.IO;
using System.Windows.Media.Imaging;
using wLabelDesigner.Models;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class PngExportTests
{
    [Fact]
    public void Render_UsesLabelDimensionsAndConfiguredDpi()
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
                    Content = "PNG",
                    Width = 20,
                    Height = 8
                }
            ]
        };

        var png = LabelPngRenderer.Render(document);
        using var stream = new MemoryStream(png, writable: false);
        var decoder = new PngBitmapDecoder(
            stream,
            BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);

        Assert.Equal([137, 80, 78, 71, 13, 10, 26, 10], png[..8]);
        Assert.Equal(203, decoder.Frames[0].PixelWidth);
        Assert.Equal(102, decoder.Frames[0].PixelHeight);
    }

    [Fact]
    public async Task ExportPngCommand_ReportsExportedFile()
    {
        var exportService = new StubExportService("C:\\Exports\\shipping-label.png");
        var viewModel = new MainViewModel(
            new StubDocumentStore(),
            new StubFileDialogService(),
            new StubPrintService(),
            new StubClipboard(),
            labelExportService: exportService);

        await viewModel.ExportPngCommand.ExecuteAsync(null);

        Assert.NotNull(exportService.Document);
        Assert.Equal("PNG exported to shipping-label.png", viewModel.StatusMessage);
    }

    private sealed class StubExportService(string? result) : ILabelExportService
    {
        public LabelDocument? Document { get; private set; }

        public Task<string?> ExportPngAsync(
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
