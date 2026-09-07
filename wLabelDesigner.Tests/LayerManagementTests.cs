using wLabelDesigner.Models;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class LayerManagementTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(45)]
    [InlineData(-45)]
    [InlineData(90)]
    [InlineData(-90)]
    [InlineData(180)]
    public void RotatedLine_CanReachEveryEdgeAndMoveBack(double angle)
    {
        var viewModel = CreateViewModel();
        var line = viewModel.AddElementAt(LabelElementKind.Line, 10, 20);
        line.DisplayRotationDegrees = angle;
        viewModel.MoveSelectedElements(-1000, -1000);
        Assert.Equal(0, line.GetMovementBounds().X, 8);
        Assert.Equal(0, line.GetMovementBounds().Y, 8);
        Assert.Equal(angle, line.DisplayRotationDegrees);

        viewModel.MoveSelectedElements(1000, 1000);
        Assert.Equal(viewModel.LabelWidth, line.GetMovementBounds().Right, 8);
        Assert.Equal(viewModel.LabelHeight, line.GetMovementBounds().Bottom, 8);
        viewModel.MoveSelectedElements(-1, -1);
        Assert.Equal(viewModel.LabelWidth - 1, line.GetMovementBounds().Right, 8);
        Assert.Equal(viewModel.LabelHeight - 1, line.GetMovementBounds().Bottom, 8);
        Assert.Equal(35, line.Width);
        Assert.Equal(0, line.Height);
    }

    [Fact]
    public void RotatedLine_NegativeBoxOriginRoundTripsWithoutMovingVisibleLine()
    {
        var viewModel = CreateViewModel();
        var line = viewModel.AddElementAt(LabelElementKind.Line, 10, 20);
        line.DisplayRotationDegrees = 90;
        viewModel.MoveSelectedElements(-1000, 0);
        Assert.True(line.X < 0);
        var reopened = new LabelElementViewModel(line.ToData());
        Assert.Equal(line.GetMovementBounds(), reopened.GetMovementBounds());
        Assert.Equal(0, reopened.GetMovementBounds().X, 8);
    }

    [Fact]
    public void OversizedRotatedLine_CanAlignEitherEdgeWithoutThrowing()
    {
        var viewModel = CreateViewModel();
        var line = viewModel.AddElementAt(LabelElementKind.Line, 10, 20);
        viewModel.FitLineCommand.Execute("Width");
        line.DisplayRotationDegrees = 90;
        viewModel.MoveSelectedElements(0, 1000);
        Assert.Equal(0, line.GetMovementBounds().Y, 8);
        viewModel.MoveSelectedElements(0, -1000);
        Assert.Equal(viewModel.LabelHeight, line.GetMovementBounds().Bottom, 8);
    }

    [Theory]
    [InlineData("Width", 100, 0)]
    [InlineData("Height", 0, 50)]
    public void FitLine_SpansLabelAndCanUndo(string direction, double width, double height)
    {
        var viewModel = CreateViewModel();
        var line = viewModel.AddElementAt(LabelElementKind.Line, 10, 10);
        line.RotationDegrees = 45;
        viewModel.FitLineCommand.Execute(direction);
        Assert.Equal(width, line.Width);
        Assert.Equal(height, line.Height);
        Assert.Equal(0, line.RotationDegrees);
        Assert.Equal(0, direction == "Width" ? line.X : line.Y);
        viewModel.UndoCommand.Execute(null);
        Assert.Equal(45, viewModel.SelectedElement!.RotationDegrees);
    }

    [Fact]
    public void LineEndpoints_CanCrossAndBecomeExactlyVertical()
    {
        var viewModel = CreateViewModel();
        var line = viewModel.AddElementAt(LabelElementKind.Line, 10, 10);
        line.RotationDegrees = 90;
        viewModel.SetLineEndpoints(line, new(20, 20), new(5, 30));
        Assert.True(line.IsLineDirectionReversed);
        Assert.Equal(0, line.RotationDegrees);
        var endpoints = LineGeometry.GetEndpoints(line.ToData());
        Assert.Equal(new LinePoint(20, 20), endpoints.End);
        viewModel.SetLineEndpoints(line, new(20, 20), new(20, 40));
        Assert.Equal(0, line.Width);
        Assert.Equal(20, line.Height);
        Assert.Equal(new LinePoint(20, 20), LineGeometry.GetEndpoints(line.ToData()).Start);
    }

    [Fact]
    public void LockedLine_CannotResizeOrFit()
    {
        var viewModel = CreateViewModel();
        var line = viewModel.AddElementAt(LabelElementKind.Line, 10, 10);
        line.IsLocked = true;
        Assert.False(viewModel.FitLineCommand.CanExecute("Width"));
        viewModel.SetLineEndpoints(line, new(0, 0), new(100, 0));
        Assert.Equal(10, line.X);
        Assert.Equal(35, line.Width);
    }

    [Theory]
    [InlineData(LabelElementKind.Line, 90)]
    [InlineData(LabelElementKind.Line, -90)]
    [InlineData(LabelElementKind.Line, 45)]
    [InlineData(LabelElementKind.Rectangle, 135)]
    public void MoveRotatedElement_PreservesRotationAndDimensions(LabelElementKind kind, double rotation)
    {
        var viewModel = CreateViewModel();
        var element = viewModel.AddElementAt(kind, 10, 20);
        element.RotationDegrees = rotation;
        var width = element.Width;
        var height = element.Height;

        viewModel.MoveSelectedElements(3, 2);
        viewModel.MoveSelectedElements(-1, -1);

        Assert.Equal(12, element.X);
        Assert.Equal(21, element.Y);
        Assert.Equal(rotation, element.RotationDegrees);
        Assert.Equal(width, element.Width);
        Assert.Equal(height, element.Height);
        Assert.Equal(rotation, element.ToData().RotationDegrees);
    }

    [Fact]
    public void BringToFront_ReordersDocumentAndLayerList()
    {
        var viewModel = CreateViewModel();
        var first = viewModel.AddElementAt(LabelElementKind.Text, 1, 1);
        var second = viewModel.AddElementAt(LabelElementKind.Rectangle, 2, 2);
        var third = viewModel.AddElementAt(LabelElementKind.QrCode, 3, 3);
        viewModel.SelectedElement = first;

        viewModel.BringToFrontCommand.Execute(null);

        Assert.Equal([second, third, first], viewModel.Elements);
        Assert.Equal([first, third, second], viewModel.LayerElements);
    }

    [Fact]
    public void PositionLockedElement_CannotMoveButCanDeleteAndChangeLayerOrder()
    {
        var viewModel = CreateViewModel();
        var locked = viewModel.AddElementAt(LabelElementKind.Text, 5, 5);
        viewModel.AddElementAt(LabelElementKind.Rectangle, 10, 10);
        viewModel.SelectedElement = locked;
        locked.IsLocked = true;

        viewModel.MoveSelectedElements(10, 10);

        Assert.Equal(5, locked.X);
        Assert.Equal(5, locked.Y);
        Assert.True(viewModel.DeleteSelectedCommand.CanExecute(null));
        Assert.True(viewModel.BringForwardCommand.CanExecute(null));

        viewModel.BringForwardCommand.Execute(null);

        Assert.Same(locked, viewModel.Elements[^1]);

        viewModel.DeleteSelectedCommand.Execute(null);

        Assert.DoesNotContain(locked, viewModel.Elements);
    }

    [Fact]
    public void LayerState_RoundTripsThroughElementData()
    {
        var element = new LabelElementViewModel(new LabelElementData
        {
            Kind = LabelElementKind.Barcode,
            IsLocked = true,
            IsVisible = false
        });

        var data = element.ToData();

        Assert.True(data.IsLocked);
        Assert.False(data.IsVisible);
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
