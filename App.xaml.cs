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

        var languageService = WpfLanguageService.Instance;
        using var welcomeViewModel = new WelcomeViewModel(languageService);
        var welcomeWindow = new WelcomeWindow(welcomeViewModel);
        if (welcomeWindow.ShowDialog() != true || welcomeViewModel.Result is null)
        {
            Shutdown();
            return;
        }

        var mainViewModel = CreateMainViewModel(languageService);
        if (welcomeViewModel.Result.Action == WelcomeAction.UseLayout &&
            welcomeViewModel.Result.Layout is { } layout)
        {
            mainViewModel.StartFromLayout(layout.CreateDocument());
        }

        var mainWindow = new MainWindow(mainViewModel);
        MainWindow = mainWindow;
        mainWindow.Show();
        ShutdownMode = ShutdownMode.OnMainWindowClose;

        if (welcomeViewModel.Result.Action == WelcomeAction.OpenExisting)
        {
            await mainViewModel.OpenCommand.ExecuteAsync(null);
        }
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
