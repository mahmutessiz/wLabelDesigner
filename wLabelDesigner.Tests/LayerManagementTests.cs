using wLabelDesigner.Models;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class LayerManagementTests
{
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
    public void LockedElement_CannotMoveDeleteOrChangeLayerOrder()
    {
        var viewModel = CreateViewModel();
        var locked = viewModel.AddElementAt(LabelElementKind.Text, 5, 5);
        viewModel.AddElementAt(LabelElementKind.Rectangle, 10, 10);
        viewModel.SelectedElement = locked;
        locked.IsLocked = true;

        viewModel.MoveSelectedElements(10, 10);

        Assert.Equal(5, locked.X);
        Assert.Equal(5, locked.Y);
        Assert.False(viewModel.DeleteSelectedCommand.CanExecute(null));
        Assert.False(viewModel.BringForwardCommand.CanExecute(null));
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
