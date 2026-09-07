namespace wLabelDesigner.Services;

public sealed record UpdateCheckResult(Version CurrentVersion, Version? LatestVersion)
{
    public bool UpdateAvailable => LatestVersion is not null && LatestVersion > CurrentVersion;
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
