using wLabelDesigner.Models;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class BarcodeFormatTests
{
    [Theory]
    [InlineData(BarcodeFormatOption.Code128, "SHIP-ABC-123")]
    [InlineData(BarcodeFormatOption.Code39, "ABC123")]
    [InlineData(BarcodeFormatOption.Ean8, "96385074")]
    [InlineData(BarcodeFormatOption.Ean13, "5901234123457")]
    [InlineData(BarcodeFormatOption.UpcA, "012345678905")]
    [InlineData(BarcodeFormatOption.Itf14, "10012345000017")]
    public void Render_CreatesEverySupportedBarcodeFormat(
        BarcodeFormatOption format,
        string content)
    {
        var bitmap = LabelImageRenderer.Render(LabelElementKind.Barcode, content, format);

        Assert.NotNull(bitmap);
        Assert.True(bitmap.PixelWidth > 0);
        Assert.True(bitmap.PixelHeight > 0);
        Assert.True(bitmap.IsFrozen);
    }

    [Theory]
    [InlineData(BarcodeFormatOption.Code128)]
    [InlineData(BarcodeFormatOption.Code39)]
    [InlineData(BarcodeFormatOption.Ean8)]
    [InlineData(BarcodeFormatOption.Ean13)]
    [InlineData(BarcodeFormatOption.UpcA)]
    [InlineData(BarcodeFormatOption.Itf14)]
    public void ViewModel_RoundTripsBarcodeFormat(BarcodeFormatOption format)
    {
        var element = new LabelElementViewModel(new LabelElementData
        {
            Kind = LabelElementKind.Barcode,
            BarcodeFormat = format
        });

        Assert.Equal(format, element.BarcodeFormat);
        Assert.Equal(format, element.ToData().BarcodeFormat);
    }

    [Fact]
    public void MainViewModel_ExposesFormatsOnlyForBarcodeSelection()
    {
        var viewModel = CreateViewModel();
        viewModel.AddElementAt(LabelElementKind.Barcode, 5, 5);

        Assert.True(viewModel.HasBarcodeSelection);
        Assert.Equal(6, viewModel.BarcodeFormats.Count);

        viewModel.AddElementAt(LabelElementKind.QrCode, 5, 5);

        Assert.False(viewModel.HasBarcodeSelection);
    }

    [Fact]
    public void ViewModel_RoundTripsHumanReadableTextVisibility()
    {
        var element = new LabelElementViewModel(new LabelElementData
        {
            Kind = LabelElementKind.Barcode,
            IsBarcodeTextVisible = false
        });

        Assert.False(element.IsBarcodeTextVisible);
        Assert.False(element.ToData().IsBarcodeTextVisible);
    }

    [Fact]
    public void Render_HumanReadableTextToggleChangesBarcodeOutput()
    {
        var withText = LabelImageRenderer.Render(
            LabelElementKind.Barcode,
            "SHIP-ABC-123",
            BarcodeFormatOption.Code128,
            isBarcodeTextVisible: true);
        var withoutText = LabelImageRenderer.Render(
            LabelElementKind.Barcode,
            "SHIP-ABC-123",
            BarcodeFormatOption.Code128,
            isBarcodeTextVisible: false);

        Assert.NotNull(withText);
        Assert.NotNull(withoutText);
        Assert.Equal(withText.PixelWidth, withoutText.PixelWidth);
        Assert.Equal(withText.PixelHeight, withoutText.PixelHeight);
        Assert.False(ReadPixels(withText).SequenceEqual(ReadPixels(withoutText)));
    }

    private static byte[] ReadPixels(System.Windows.Media.Imaging.BitmapSource bitmap)
    {
        var stride = ((bitmap.PixelWidth * bitmap.Format.BitsPerPixel) + 7) / 8;
        var pixels = new byte[stride * bitmap.PixelHeight];
        bitmap.CopyPixels(pixels, stride, 0);
        return pixels;
    }

    private static MainViewModel CreateViewModel() => new(
        new StubDocumentStore(),
        new StubFileDialogService(),
        new StubPrintService(),
        new StubClipboard());

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
