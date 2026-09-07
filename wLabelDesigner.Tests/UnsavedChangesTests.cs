using wLabelDesigner.Models;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class UnsavedChangesTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CheckForUpdates_ShowsResultOrErrorAndPreservesDocument(bool fail)
    {
        var notifications = new StubUpdateNotifications();
        var viewModel = new MainViewModel(new StubDocumentStore(), new StubFileDialogService(),
            new StubPrintService(), new StubClipboard(),
            updateService: new StubUpdateService(fail), updateNotificationService: notifications);
        var element = viewModel.AddElementAt(LabelElementKind.Text, 1, 1);

        await viewModel.CheckForUpdatesCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsDirty);
        Assert.Contains(element, viewModel.Elements);
        Assert.Equal(fail, notifications.Error is not null);
        Assert.Equal(!fail, notifications.Result?.UpdateAvailable == true);
        Assert.True(viewModel.CheckForUpdatesCommand.CanExecute(null));
    }

    private sealed class StubUpdateService(bool fail) : IUpdateService
    {
        public Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default) => fail
            ? Task.FromException<UpdateCheckResult>(new System.Net.Http.HttpRequestException("Offline"))
            : Task.FromResult(new UpdateCheckResult(new Version(1, 0), new Version(2, 0)));
    }

    private sealed class StubUpdateNotifications : IUpdateNotificationService
    {
        public UpdateCheckResult? Result { get; private set; }
        public string? Error { get; private set; }
        public void ShowResult(UpdateCheckResult result) => Result = result;
        public void ShowError(string message) => Error = message;
    }

    [Fact]
    public async Task SaveAs_UsesChosenPathForSubsequentSaves()
    {
        var store = new StubDocumentStore();
        var dialogs = new StubFileDialogService { SavePath = "copy.wld" };
        var viewModel = CreateViewModel(store, dialogs);
        await viewModel.OpenPathAsync("original.wld");
        viewModel.AddElementAt(LabelElementKind.Text, 4, 5);

        await viewModel.SaveAsCommand.ExecuteAsync(null);

        Assert.Equal("copy.wld", store.SavedPath);
        Assert.Equal("original.wld", dialogs.SuggestedName);
        Assert.Single(store.SavedDocument!.Elements);
        Assert.False(viewModel.IsDirty);
        Assert.Equal("Saved copy.wld", viewModel.StatusMessage);

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("copy.wld", store.SavedPath);
        Assert.Equal(1, dialogs.SaveDialogCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveAs_WhenCancelledOrFailed_PreservesOriginalPathAndChanges(bool fail)
    {
        var store = new StubDocumentStore { FailSave = fail };
        var dialogs = new StubFileDialogService { SavePath = fail ? "copy.wld" : null };
        var viewModel = CreateViewModel(store, dialogs);
        await viewModel.OpenPathAsync("original.wld");
        viewModel.AddElementAt(LabelElementKind.Text, 4, 5);

        await viewModel.SaveAsCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsDirty);
        Assert.Null(store.SavedDocument);
        Assert.StartsWith(fail ? "Could not save template:" : "Save cancelled", viewModel.StatusMessage);

        store.FailSave = false;
        await viewModel.SaveCommand.ExecuteAsync(null);
        Assert.Equal("original.wld", store.SavedPath);
        Assert.Equal(1, dialogs.SaveDialogCount);
    }

    [Fact]
    public async Task SaveAs_ForNewDocument_SavesEditableTemplate()
    {
        var store = new StubDocumentStore();
        var dialogs = new StubFileDialogService { SavePath = "new.wld" };
        var viewModel = CreateViewModel(store, dialogs);
        viewModel.AddElementAt(LabelElementKind.Text, 4, 5);

        await viewModel.SaveAsCommand.ExecuteAsync(null);

        Assert.Equal("new.wld", store.SavedPath);
        Assert.EndsWith(".wld", dialogs.SuggestedName);
        Assert.Single(store.SavedDocument!.Elements);
        Assert.False(viewModel.IsDirty);
    }

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
        var dialogs = new StubFileDialogService { SavePath = "saved.wld" };
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
        var dialogs = new StubFileDialogService { OpenPath = "opened.wld" };
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
        public string? SavedPath { get; private set; }
        public bool FailSave { get; set; }

        public Task<LabelDocument> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(DocumentToLoad);

        public Task SaveAsync(string path, LabelDocument document, CancellationToken cancellationToken = default)
        {
            if (FailSave)
            {
                throw new System.IO.IOException("Test write failure");
            }
            SavedPath = path;
            SavedDocument = document;
            return Task.CompletedTask;
        }
    }

    private sealed class StubFileDialogService : IFileDialogService
    {
        public string? OpenPath { get; init; }

        public string? SavePath { get; init; }

        public string? ChooseTemplateToOpen() => OpenPath;

        public string? SuggestedName { get; private set; }
        public int SaveDialogCount { get; private set; }

        public string? ChooseTemplateToSave(string suggestedFileName)
        {
            SuggestedName = suggestedFileName;
            SaveDialogCount++;
            return SavePath;
        }
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
