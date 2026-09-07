using System.Diagnostics;
using System.Windows;

namespace wLabelDesigner.Services;

public sealed class WpfUpdateNotificationService(ILanguageService language) : IUpdateNotificationService
{
    private string Title => language.IsTurkish ? "Güncellemeleri denetle" : "Check for Updates";

    public void ShowResult(UpdateCheckResult result)
    {
        if (result.UpdateAvailable)
        {
            var message = language.IsTurkish
                ? $"Yeni sürüm: {result.LatestVersion}\nYüklü sürüm: {result.CurrentVersion}\n\nİndirmek için GitHub sürüm sayfası açılsın mı?"
                : $"New version: {result.LatestVersion}\nInstalled version: {result.CurrentVersion}\n\nOpen the GitHub release page to download it?";
            if (MessageBox.Show(Application.Current.MainWindow, message, Title,
                    MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo(
                    "https://github.com/mahmutessiz/wLabelDesigner/releases/latest") { UseShellExecute = true });
            }
            return;
        }

        var status = result.LatestVersion is null
            ? language.IsTurkish ? "Yayınlanmış kararlı sürüm bulunamadı." : "No published stable release was found."
            : language.IsTurkish ? "Uygulamanız güncel." : "Your app is up to date.";
        MessageBox.Show(Application.Current.MainWindow, $"{status}\n\n{result.CurrentVersion}", Title,
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowError(string message) => MessageBox.Show(Application.Current.MainWindow,
        message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
}
