namespace wLabelDesigner.Services;

public sealed record UpdateCheckResult(Version CurrentVersion, Version? LatestVersion, UpdateInstaller? Installer = null)
{
    public bool UpdateAvailable => LatestVersion is not null && LatestVersion > CurrentVersion;
}

public sealed record UpdateInstaller(Uri DownloadUri, long Size, string Sha256);

public interface IUpdateInstallerService
{
    Task<string> DownloadAsync(UpdateInstaller installer, IProgress<double> progress, CancellationToken cancellationToken);
    Task<bool> InstallAsync(string path);
}

public interface IUpdateService
{
    Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}

public interface IUpdateNotificationService
{
    void ShowResult(UpdateCheckResult result);
    void ShowError(string message);
}
