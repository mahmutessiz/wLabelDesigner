using System.Windows;
using wLabelDesigner.ViewModels;

namespace wLabelDesigner.Services;

public sealed class WpfUpdateNotificationService(ILanguageService language, IUpdateInstallerService installerService) : IUpdateNotificationService
{
    public void ShowResult(UpdateCheckResult result) =>
        Show(new UpdateViewModel(result, installerService, language.IsTurkish));

    public void ShowError(string message) => Show(new UpdateViewModel(
        new UpdateCheckResult(typeof(App).Assembly.GetName().Version ?? new Version(1, 0), null),
        installerService, language.IsTurkish, message));

    private static void Show(UpdateViewModel viewModel) => new UpdateWindow(viewModel)
    {
        Owner = Application.Current.MainWindow
    }.ShowDialog();
}
