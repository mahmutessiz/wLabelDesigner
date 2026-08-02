using wLabelDesigner.Models;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class ImageElementTests
{
    private const string OnePixelPng =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    [Fact]
    public void AddImageAt_EmbedsImageAndPreservesAspectRatio()
    {
        var viewModel = CreateViewModel(new StubImageImportService(
            new ImportedImage(OnePixelPng, 800, 400)));

        var element = viewModel.AddImageAt(12, 8);

        Assert.NotNull(element);
        Assert.Equal(LabelElementKind.Image, element.Kind);
        Assert.Equal("Image", element.DisplayName);
        Assert.Equal(OnePixelPng, element.Content);
        Assert.Equal(2, element.Width / element.Height, precision: 5);
        Assert.Equal(12, element.X);
        Assert.Equal(8, element.Y);
        Assert.Equal(OnePixelPng, element.ToData().Content);
    }

    [Fact]
    public void AddImageAt_WhenImportIsCancelled_DoesNotAddElement()
    {
        var viewModel = CreateViewModel(new StubImageImportService(null));

        var element = viewModel.AddImageAt(5, 5);

        Assert.Null(element);
        Assert.Empty(viewModel.Elements);
        Assert.Equal("Image import cancelled", viewModel.StatusMessage);
    }

    [Fact]
    public void Render_EmbeddedImage_ReturnsBitmap()
    {
        var image = LabelImageRenderer.Render(LabelElementKind.Image, OnePixelPng);

        Assert.NotNull(image);
        Assert.Equal(1, image.PixelWidth);
        Assert.Equal(1, image.PixelHeight);
    }

    [Fact]
    public void Render_InvalidEmbeddedImage_ReturnsNull()
    {
        Assert.Null(LabelImageRenderer.Render(LabelElementKind.Image, "not an image"));
        Assert.Null(LabelImageRenderer.Render(LabelElementKind.Image, "data:image/png;base64,invalid"));
    }

    private static MainViewModel CreateViewModel(IImageImportService imageImportService) => new(
        new StubDocumentStore(),
        new StubFileDialogService(),
        new StubPrintService(),
        new StubClipboard(),
        imageImportService);

    private sealed class StubImageImportService(ImportedImage? image) : IImageImportService
    {
        public ImportedImage? ImportImage() => image;
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
