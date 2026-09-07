using System.ComponentModel;
using System.IO;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using wLabelDesigner.Services;

namespace wLabelDesigner.ViewModels;

public sealed partial class UpdateViewModel : ObservableObject
{
    private readonly UpdateCheckResult result;
    private readonly IUpdateInstallerService installerService;
    private readonly bool turkish;
    private string? downloadedPath;
    private readonly string? errorMessage;

    public UpdateViewModel(UpdateCheckResult result, IUpdateInstallerService installerService, bool turkish, string? errorMessage = null)
    {
        this.result = result;
        this.installerService = installerService;
        this.turkish = turkish;
        this.errorMessage = errorMessage;
        Status = result.Installer is null
            ? Text("This release has no compatible installer. Please try again later.", "Bu sürüm için uygun yükleyici bulunamadı. Lütfen daha sonra tekrar deneyin.")
            : Text("Download the update, then setup will guide you through installation.", "Güncellemeyi indirin; kurulum sihirbazı yükleme boyunca size yardımcı olacak.");
    }

    public string Title => errorMessage is not null ? Text("Couldn't check for updates", "Güncellemeler denetlenemedi")
        : result.UpdateAvailable ? Text("A fresh update is ready", "Yeni bir güncelleme hazır")
        : result.LatestVersion is not null ? Text("You're up to date", "Uygulamanız güncel")
        : Text("No updates just yet", "Henüz güncelleme yok");
    public string Description => errorMessage ?? (!result.UpdateAvailable
        ? result.LatestVersion is null
            ? Text("There are no published updates yet. Check back again soon.", "Henüz yayınlanmış bir güncelleme yok. Daha sonra tekrar kontrol edin.")
            : Text("You're running the latest version of wLabelDesigner. You're all set to keep creating.", "wLabelDesigner'ın en güncel sürümünü kullanıyorsunuz. Tasarlamaya devam edebilirsiniz.")
        : Text("Keep your label designer up to date. We'll take care of the download and open setup for you.", "Etiket tasarımcınızı güncel tutun. Güncellemeyi indirip kurulumu sizin için açacağız."));
    public bool ShowDownload => errorMessage is null && result.UpdateAvailable;
    public bool ShowVersions => errorMessage is null;
    public string IconGlyph => errorMessage is not null ? "\uE783" : result.UpdateAvailable ? "\uE896" : "\uE73E";
    public string Eyebrow => Text("SOFTWARE UPDATE", "YAZILIM GÜNCELLEMESİ");
    public string CurrentVersionLabel => Text("Installed version", "Yüklü sürüm");
    public string LatestVersionLabel => Text("Latest version", "Son sürüm");
    public string CurrentVersionText => FormatVersion(result.CurrentVersion);
    public string LatestVersionText => result.LatestVersion is null ? "—" : FormatVersion(result.LatestVersion);
    public string CloseLabel => ShowDownload ? Text("Not now", "Şimdi değil") : Text("Done", "Tamam");
    public string CloseWindowLabel => Text("Close", "Kapat");
    public string ProgressLabel => Text("Download progress", "İndirme ilerlemesi");
    public string FooterNote => Text("Your labels stay safe. We'll ask you to save before setup starts.", "Etiketleriniz güvende. Kurulumdan önce kaydetmenizi isteyeceğiz.");
    private static string FormatVersion(Version version) => version.Revision > 0
        ? version.ToString() : $"{version.Major}.{version.Minor}.{Math.Max(0, version.Build)}";
    public string Versions => Text($"Installed: {result.CurrentVersion}   New: {result.LatestVersion}",
        $"Yüklü: {result.CurrentVersion}   Yeni: {result.LatestVersion}");
    public string DownloadLabel => downloadedPath is null ? Text("Download Update", "Güncellemeyi İndir") : Text("Install Update", "Güncellemeyi Yükle");
    public string CancelLabel => Text("Cancel download", "İndirmeyi iptal et");
    [ObservableProperty] private string status = string.Empty;
    [ObservableProperty] private double progress;
    [ObservableProperty] private bool isBusy;

    private bool CanDownload() => ShowDownload && result.Installer is not null;

    [RelayCommand(CanExecute = nameof(CanDownload), IncludeCancelCommand = true)]
    private async Task DownloadAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        try
        {
            if (downloadedPath is null)
            {
                Status = Text("Downloading update…", "Güncelleme indiriliyor…");
                Progress = 0;
                downloadedPath = await installerService.DownloadAsync(result.Installer!,
                    new Progress<double>(value => Progress = value), cancellationToken);
                OnPropertyChanged(nameof(DownloadLabel));
            }
            cancellationToken.ThrowIfCancellationRequested();
            Status = Text("Starting setup…", "Kurulum başlatılıyor…");
            IsBusy = false;
            var started = await installerService.InstallAsync(downloadedPath);
            Status = started ? Text("Setup started.", "Kurulum başlatıldı.")
                : Text("Installation cancelled. Your download is ready when you are.", "Kurulum iptal edildi. İndirme yüklemeye hazır.");
        }
        catch (OperationCanceledException)
        {
            Status = Text("Download cancelled or timed out. You can try again.", "İndirme iptal edildi veya zaman aşımına uğradı. Tekrar deneyebilirsiniz.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or HttpRequestException or Win32Exception or InvalidOperationException)
        {
            Status = Text("The update could not be downloaded or started. Please check your connection and try again.",
                "Güncelleme indirilemedi veya başlatılamadı. Bağlantınızı kontrol edip tekrar deneyin.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string Text(string english, string translation) => turkish ? translation : english;
}
