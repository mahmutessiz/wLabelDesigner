using wLabelDesigner.Models;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class PredefinedLabelLayoutTests
{
    [Fact]
    public void StarterLayouts_HaveSupportedDimensionsAndBoundedElements()
    {
        Assert.Equal(4, PredefinedLabelLayouts.All.Count);

        foreach (var layout in PredefinedLabelLayouts.All)
        {
            var document = layout.CreateDocument();

            Assert.Equal(layout.WidthMillimeters, document.WidthMillimeters);
            Assert.Equal(layout.HeightMillimeters, document.HeightMillimeters);
            Assert.Equal(layout.PrinterDpi, document.PrinterDpi);
            Assert.NotEmpty(document.Elements);
            Assert.All(document.Elements, element =>
            {
                Assert.InRange(element.X, 0, document.WidthMillimeters);
                Assert.InRange(element.Y, 0, document.HeightMillimeters);
                Assert.True(element.Width > 0);
                Assert.True(element.Height > 0);
                Assert.True(element.X + element.Width <= document.WidthMillimeters);
                Assert.True(element.Y + element.Height <= document.HeightMillimeters);
            });
        }
    }

    [Fact]
    public void CreateDocument_ReturnsIndependentDocuments()
    {
        var layout = PredefinedLabelLayouts.All[0];

        var first = layout.CreateDocument();
        var second = layout.CreateDocument();

        Assert.NotSame(first, second);
        Assert.NotEqual(first.Elements[0].Id, second.Elements[0].Id);
    }

    [Fact]
    public void StartFromLayout_LoadsEditableUnsavedDocument()
    {
        var viewModel = new MainViewModel(
            new StubDocumentStore(),
            new StubFileDialogService(),
            new StubPrintService(),
            new StubClipboard());
        var document = PredefinedLabelLayouts.All[1].CreateDocument();

        viewModel.StartFromLayout(document);

        Assert.Equal(document.Name, viewModel.DocumentName);
        Assert.Equal(document.WidthMillimeters, viewModel.LabelWidth);
        Assert.Equal(document.HeightMillimeters, viewModel.LabelHeight);
        Assert.Equal(document.Elements.Count, viewModel.Elements.Count);
        Assert.True(viewModel.IsDirty);
    }

    private sealed class StubDocumentStore : ILabelDocumentStore
    {
        public Task<LabelDocument> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveAsync(string path, LabelDocument document, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
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
