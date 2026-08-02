using wLabelDesigner.Models;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class BasicShapeTests
{
    [Theory]
    [InlineData(LabelElementKind.Ellipse, "Ellipse", 30, 20)]
    [InlineData(LabelElementKind.Triangle, "Triangle", 30, 24)]
    [InlineData(LabelElementKind.Diamond, "Diamond", 30, 24)]
    public void AddElementAt_CreatesEditableShape(
        LabelElementKind kind,
        string displayName,
        double expectedWidth,
        double expectedHeight)
    {
        var viewModel = CreateViewModel();

        var element = viewModel.AddElementAt(kind, 7, 8);

        Assert.Equal(displayName, element.DisplayName);
        Assert.Equal(expectedWidth, element.Width);
        Assert.Equal(expectedHeight, element.Height);
        Assert.True(viewModel.HasShapeSelection);
        Assert.Equal(kind, element.ToData().Kind);
    }

    [Theory]
    [InlineData(LabelElementKind.Ellipse)]
    [InlineData(LabelElementKind.Triangle)]
    [InlineData(LabelElementKind.Diamond)]
    public void CreateDrawing_RendersBasicShape(LabelElementKind kind)
    {
        var document = new LabelDocument
        {
            WidthMillimeters = 50,
            HeightMillimeters = 30,
            Elements =
            [
                new LabelElementData
                {
                    Kind = kind,
                    X = 5,
                    Y = 5,
                    Width = 20,
                    Height = 15,
                    StrokeThickness = 2
                }
            ]
        };

        var drawing = LabelDrawingRenderer.CreateDrawing(document);

        Assert.False(drawing.Bounds.IsEmpty);
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
