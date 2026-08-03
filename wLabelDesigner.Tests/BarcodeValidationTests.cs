using wLabelDesigner.Models;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class BarcodeValidationTests
{
    [Theory]
    [InlineData(BarcodeFormatOption.Code128, "SHIP-abc-123")]
    [InlineData(BarcodeFormatOption.Code39, "SHIP-ABC 123")]
    [InlineData(BarcodeFormatOption.Ean8, "96385074")]
    [InlineData(BarcodeFormatOption.Ean13, "5901234123457")]
    [InlineData(BarcodeFormatOption.UpcA, "012345678905")]
    [InlineData(BarcodeFormatOption.Itf14, "10012345000017")]
    public void IsValid_AcceptsValidContent(BarcodeFormatOption format, string content)
    {
        Assert.True(BarcodeContentValidator.IsValid(format, content));
        Assert.Null(BarcodeContentValidator.GetError(format, content));
    }

    [Theory]
    [InlineData(BarcodeFormatOption.Code128, "Barkod-ğ", "Code 128 supports printable ASCII characters only.")]
    [InlineData(BarcodeFormatOption.Code39, "lowercase", "Code 39 supports A-Z, 0-9, space, and - . $ / + % only.")]
    [InlineData(BarcodeFormatOption.Ean8, "9638507", "EAN-8 requires exactly 8 digits.")]
    [InlineData(BarcodeFormatOption.Ean8, "96385075", "EAN-8 check digit should be 4.")]
    [InlineData(BarcodeFormatOption.Ean13, "5901234123458", "EAN-13 check digit should be 7.")]
    [InlineData(BarcodeFormatOption.UpcA, "012345678906", "UPC-A check digit should be 5.")]
    [InlineData(BarcodeFormatOption.Itf14, "10012345000018", "ITF-14 check digit should be 7.")]
    public void GetError_ExplainsInvalidContent(
        BarcodeFormatOption format,
        string content,
        string expectedError)
    {
        Assert.Equal(expectedError, BarcodeContentValidator.GetError(format, content));
    }

    [Fact]
    public void ViewModel_RevalidatesWhenFormatOrContentChanges()
    {
        var element = new LabelElementViewModel(new LabelElementData
        {
            Kind = LabelElementKind.Barcode,
            Content = "123456789012"
        });

        Assert.True(element.IsBarcodeContentValid);

        element.BarcodeFormat = BarcodeFormatOption.Ean13;

        Assert.False(element.IsBarcodeContentValid);
        Assert.Equal("EAN-13 requires exactly 13 digits.", element.BarcodeValidationError);

        element.Content = "5901234123457";

        Assert.True(element.IsBarcodeContentValid);
        Assert.Null(element.BarcodeValidationError);
    }

    [Fact]
    public void Renderer_DoesNotRenderInvalidBarcodeContent()
    {
        var bitmap = LabelImageRenderer.Render(
            LabelElementKind.Barcode,
            "5901234123458",
            BarcodeFormatOption.Ean13);

        Assert.Null(bitmap);
    }

    [Fact]
    public void Print_StopsAndSelectsInvalidBarcode()
    {
        var printService = new StubPrintService();
        var viewModel = CreateViewModel(printService);
        var invalidBarcode = viewModel.AddElementAt(LabelElementKind.Barcode, 5, 5);
        invalidBarcode.BarcodeFormat = BarcodeFormatOption.Ean13;
        invalidBarcode.Content = "123";
        viewModel.AddElementAt(LabelElementKind.Text, 10, 10);

        viewModel.PrintCommand.Execute(null);

        Assert.Equal(0, printService.PrintCalls);
        Assert.Same(invalidBarcode, viewModel.SelectedElement);
        Assert.Equal("Cannot print: EAN-13 requires exactly 13 digits.", viewModel.StatusMessage);
    }

    private static MainViewModel CreateViewModel(StubPrintService printService) => new(
        new StubDocumentStore(),
        new StubFileDialogService(),
        printService,
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
        public int PrintCalls { get; private set; }

        public bool Print(LabelDocument document)
        {
            PrintCalls++;
            return true;
        }
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
