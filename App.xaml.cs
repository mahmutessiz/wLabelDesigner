using System.IO;
using System.Windows;
using wLabelDesigner.Models;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;

namespace wLabelDesigner;

public partial class App : Application
{
    private static readonly System.Net.Http.HttpClient UpdateHttpClient = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly IRecentFilesService recentFilesService = new JsonRecentFilesService();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var startupTemplatePath = StartupTemplatePathResolver.Resolve(e.Args);
        if (startupTemplatePath is null)
        {
            await ShowWelcomeAsync();
            return;
        }

        await ShowStartupTemplateAsync(startupTemplatePath);
    }

    private async Task ShowStartupTemplateAsync(string path)
    {
        var mainViewModel = CreateMainViewModel(WpfLanguageService.Instance);
        await mainViewModel.OpenPathAsync(path);
        ShowMainWindow(mainViewModel);
    }

    private async Task ShowWelcomeAsync(MainWindow? previousWindow = null)
    {
        var languageService = WpfLanguageService.Instance;
        var recentFilePaths = await TryGetRecentFilesAsync();
        using var welcomeViewModel = new WelcomeViewModel(languageService, recentFilePaths);
        var welcomeWindow = new WelcomeWindow(welcomeViewModel);
        if (welcomeWindow.ShowDialog() != true || welcomeViewModel.Result is null)
        {
            if (previousWindow is null)
            {
                Shutdown();
            }
            else
            {
                RestoreMainWindow(previousWindow);
            }

            return;
        }

        if (previousWindow?.DataContext is MainViewModel { IsDirty: true } previousViewModel)
        {
            previousWindow.Show();
            previousWindow.Activate();
            if (!await previousViewModel.ConfirmCloseAsync())
            {
                RestoreMainWindow(previousWindow);
                return;
            }

            previousWindow.Hide();
        }

        previousWindow?.CloseWithoutPrompt();

        var mainViewModel = CreateMainViewModel(languageService);
        if (welcomeViewModel.Result.Action == WelcomeAction.UseLayout &&
            welcomeViewModel.Result.Layout is { } layout)
        {
            mainViewModel.StartFromLayout(layout.CreateDocument());
        }

        ShowMainWindow(mainViewModel);

        if (welcomeViewModel.Result.Action == WelcomeAction.OpenExisting)
        {
            await mainViewModel.OpenCommand.ExecuteAsync(null);
        }
        else if (welcomeViewModel.Result is { Action: WelcomeAction.OpenRecent, Path: not null } recentResult)
        {
            await mainViewModel.OpenPathAsync(recentResult.Path);
        }
    }

    private void ShowMainWindow(MainViewModel mainViewModel)
    {
        var mainWindow = new MainWindow(mainViewModel);
        mainWindow.WelcomeScreenRequested += MainWindow_WelcomeScreenRequested;
        MainWindow = mainWindow;
        mainWindow.Show();
        ShutdownMode = ShutdownMode.OnMainWindowClose;
    }

    private async void MainWindow_WelcomeScreenRequested(object? sender, EventArgs e)
    {
        if (sender is not MainWindow mainWindow || !mainWindow.IsVisible)
        {
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        mainWindow.Hide();
        await ShowWelcomeAsync(mainWindow);
    }

    private void RestoreMainWindow(MainWindow mainWindow)
    {
        MainWindow = mainWindow;
        mainWindow.Show();
        mainWindow.Activate();
        ShutdownMode = ShutdownMode.OnMainWindowClose;
    }

    private async Task<IReadOnlyList<string>> TryGetRecentFilesAsync()
    {
        try
        {
            return await recentFilesService.GetRecentFilesAsync();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private MainViewModel CreateMainViewModel(ILanguageService languageService) => new(
        new JsonLabelDocumentStore(),
        new FileDialogService(),
        new WpfLabelPrintService(),
        new WpfElementClipboard(),
        new WpfImageImportService(),
        new WpfLabelExportService(),
        new WpfUnsavedChangesPromptService(),
        languageService,
        recentFilesService,
        new GitHubUpdateService(UpdateHttpClient, typeof(App).Assembly.GetName().Version ?? new Version(1, 0, 0)),
        new WpfUpdateNotificationService(languageService, new UpdateInstallerService(
            UpdateHttpClient,
            Path.Combine(Path.GetTempPath(), "wLabelDesigner", "Updates"),
            StartUpdateInstallerAsync)));

    private async Task<bool> StartUpdateInstallerAsync(string path)
    {
        if (MainWindow is not MainWindow window || window.DataContext is not MainViewModel viewModel ||
            !await viewModel.ConfirmCloseAsync())
        {
            return false;
        }

        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path)
        {
            UseShellExecute = true
        });
        if (process is null)
        {
            throw new InvalidOperationException("Could not start the update installer.");
        }
        window.CloseWithoutPrompt();
        return true;
    }
}
