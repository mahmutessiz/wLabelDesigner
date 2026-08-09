using System.IO;
using wLabelDesigner.Models;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class RecentFilesTests
{
    [Fact]
    public async Task JsonService_PersistsMostRecentUniqueExistingFilesWithinLimit()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"wLabelDesigner-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var paths = Enumerable.Range(0, JsonRecentFilesService.MaximumRecentFiles + 2)
                .Select(index => Path.Combine(directory, $"label-{index}.wld"))
                .ToArray();
            foreach (var path in paths)
            {
                await File.WriteAllTextAsync(path, "{}");
            }

            var service = new JsonRecentFilesService(directory);
            foreach (var path in paths)
            {
                await service.AddRecentFileAsync(path);
            }

            await service.AddRecentFileAsync(paths[5]);
            var recentFiles = await new JsonRecentFilesService(directory).GetRecentFilesAsync();

            Assert.Equal(JsonRecentFilesService.MaximumRecentFiles, recentFiles.Count);
            Assert.Equal(Path.GetFullPath(paths[5]), recentFiles[0]);
            Assert.Equal(recentFiles.Count, recentFiles.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.DoesNotContain(Path.GetFullPath(paths[0]), recentFiles);

            File.Delete(paths[5]);
            recentFiles = await service.GetRecentFilesAsync();
            Assert.DoesNotContain(Path.GetFullPath(paths[5]), recentFiles);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void WelcomeViewModel_OpenRecentCompletesWithSelectedPath()
    {
        const string path = @"C:\Labels\shipping.wld";
        using var viewModel = new WelcomeViewModel(new StubLanguageService(), [path]);
        var closeRequested = false;
        viewModel.RequestClose += (_, _) => closeRequested = true;

        viewModel.OpenRecentCommand.Execute(viewModel.RecentFiles[0]);

        Assert.True(closeRequested);
        Assert.Equal(WelcomeAction.OpenRecent, viewModel.Result?.Action);
        Assert.Equal(path, viewModel.Result?.Path);
    }

    [Fact]
    public async Task MainViewModel_SuccessfulOpenAndSaveUpdateRecentFiles()
    {
        const string openPath = @"C:\Labels\opened.wld";
        const string savePath = @"C:\Labels\saved.wld";
        var recentFiles = new StubRecentFilesService();
        var viewModel = new MainViewModel(
            new StubDocumentStore(),
            new StubFileDialogService(savePath),
            new StubPrintService(),
            new StubClipboard(),
            recentFilesService: recentFiles);

        Assert.True(await viewModel.OpenPathAsync(openPath));
        await viewModel.NewDocumentCommand.ExecuteAsync(null);
        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal([openPath, savePath], recentFiles.AddedPaths);
    }

    private sealed class StubLanguageService : ILanguageService
    {
        public string CurrentLanguage => "en";

        public bool IsTurkish => false;

        public event EventHandler? LanguageChanged
        {
            add { }
            remove { }
        }

        public void SetLanguage(string language)
        {
        }

        public string Translate(string text) => text;
    }

    private sealed class StubRecentFilesService : IRecentFilesService
    {
        public List<string> AddedPaths { get; } = [];

        public Task<IReadOnlyList<string>> GetRecentFilesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public Task AddRecentFileAsync(string path, CancellationToken cancellationToken = default)
        {
            AddedPaths.Add(path);
            return Task.CompletedTask;
        }

        public Task RemoveRecentFileAsync(string path, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubDocumentStore : ILabelDocumentStore
    {
        public Task<LabelDocument> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            Task.FromResult(new LabelDocument { Name = "Opened" });

        public Task SaveAsync(string path, LabelDocument document, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubFileDialogService(string savePath) : IFileDialogService
    {
        public string? ChooseTemplateToOpen() => null;

        public string? ChooseTemplateToSave(string suggestedFileName) => savePath;
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
