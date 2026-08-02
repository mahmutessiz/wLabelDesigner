using wLabelDesigner.Models;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class UnsavedChangesTests
{
    [Fact]
    public async Task NewDocument_WhenPromptIsCancelled_PreservesCurrentDocument()
    {
        var prompt = new StubPrompt(UnsavedChangesChoice.Cancel);
        var viewModel = CreateViewModel(prompt: prompt);
        var element = viewModel.AddElementAt(LabelElementKind.Text, 4, 5);

        await viewModel.NewDocumentCommand.ExecuteAsync(null);

        Assert.Contains(element, viewModel.Elements);
        Assert.True(viewModel.IsDirty);
        Assert.Equal("Action cancelled", viewModel.StatusMessage);
        Assert.Equal(1, prompt.CallCount);
    }

    [Fact]
    public async Task NewDocument_WhenSaveIsChosen_SavesBeforeReplacingDocument()
    {
        var store = new StubDocumentStore();
        var dialogs = new StubFileDialogService { SavePath = "saved.fckbartndr" };
        var viewModel = CreateViewModel(
            store,
            dialogs,
            new StubPrompt(UnsavedChangesChoice.Save));
        viewModel.AddElementAt(LabelElementKind.Barcode, 4, 5);

        await viewModel.NewDocumentCommand.ExecuteAsync(null);

        Assert.NotNull(store.SavedDocument);
        Assert.Single(store.SavedDocument.Elements);
        Assert.Empty(viewModel.Elements);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public async Task Open_WhenDiscardIsChosen_ReplacesCurrentDocument()
    {
        var store = new StubDocumentStore
        {
            DocumentToLoad = new LabelDocument
            {
                Name = "Opened label",
                Elements =
                [
                    new LabelElementData
                    {
                        Kind = LabelElementKind.Text,
                        Content = "Loaded"
                    }
                ]
            }
        };
        var dialogs = new StubFileDialogService { OpenPath = "opened.fckbartndr" };
        var viewModel = CreateViewModel(
            store,
            dialogs,
            new StubPrompt(UnsavedChangesChoice.Discard));
        viewModel.AddElementAt(LabelElementKind.Rectangle, 1, 1);

        await viewModel.OpenCommand.ExecuteAsync(null);

        Assert.Equal("Opened label", viewModel.DocumentName);
        Assert.Single(viewModel.Elements);
        Assert.Equal("Loaded", viewModel.Elements[0].Content);
        Assert.False(viewModel.IsDirty);
    }

    [Fact]
    public async Task ConfirmClose_WhenDocumentIsClean_DoesNotPrompt()
    {
        var prompt = new StubPrompt(UnsavedChangesChoice.Cancel);
        var viewModel = CreateViewModel(prompt: prompt);

        var canClose = await viewModel.ConfirmCloseAsync();

        Assert.True(canClose);
        Assert.Equal(0, prompt.CallCount);
    }

    private static MainViewModel CreateViewModel(
        StubDocumentStore? store = null,
        StubFileDialogService? dialogs = null,
        IUnsavedChangesPromptService? prompt = null) => new(
        store ?? new StubDocumentStore(),
        dialogs ?? new StubFileDialogService(),
        new StubPrintService(),
        new StubClipboard(),
        unsavedChangesPromptService: prompt);

    private sealed class StubPrompt(UnsavedChangesChoice choice) : IUnsavedChangesPromptService
    {
        public int CallCount { get; private set; }

        public UnsavedChangesChoice ConfirmSaveChanges(string documentName)
        {
            CallCount++;
            return choice;
        }
    }

    private sealed class StubDocumentStore : ILabelDocumentStore
    {
        public LabelDocument DocumentToLoad { get; set; } = new();

        public LabelDocument? SavedDocument { get; private set; }

        public Task<LabelDocument> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(DocumentToLoad);

        public Task SaveAsync(string path, LabelDocument document, CancellationToken cancellationToken = default)
        {
            SavedDocument = document;
            return Task.CompletedTask;
        }
    }

    private sealed class StubFileDialogService : IFileDialogService
    {
        public string? OpenPath { get; init; }

        public string? SavePath { get; init; }

        public string? ChooseTemplateToOpen() => OpenPath;

        public string? ChooseTemplateToSave(string suggestedFileName) => SavePath;
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
