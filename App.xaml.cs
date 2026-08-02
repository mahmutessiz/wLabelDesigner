using System.Windows;
using wLabelDesigner.Models;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;

namespace wLabelDesigner;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        await ShowWelcomeAsync();
    }

    private async Task ShowWelcomeAsync(MainWindow? previousWindow = null)
    {
        var languageService = WpfLanguageService.Instance;
        using var welcomeViewModel = new WelcomeViewModel(languageService);
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

        var mainWindow = new MainWindow(mainViewModel);
        mainWindow.WelcomeScreenRequested += MainWindow_WelcomeScreenRequested;
        MainWindow = mainWindow;
        mainWindow.Show();
        ShutdownMode = ShutdownMode.OnMainWindowClose;

        if (welcomeViewModel.Result.Action == WelcomeAction.OpenExisting)
        {
            await mainViewModel.OpenCommand.ExecuteAsync(null);
        }
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

    private static MainViewModel CreateMainViewModel(ILanguageService languageService) => new(
        new JsonLabelDocumentStore(),
        new FileDialogService(),
        new WpfLabelPrintService(),
        new WpfElementClipboard(),
        new WpfImageImportService(),
        new WpfLabelExportService(),
        new WpfUnsavedChangesPromptService(),
        languageService);
}
