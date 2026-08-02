namespace wLabelDesigner.Services;

public interface IRecentFilesService
{
    Task<IReadOnlyList<string>> GetRecentFilesAsync(CancellationToken cancellationToken = default);

    Task AddRecentFileAsync(string path, CancellationToken cancellationToken = default);

    Task RemoveRecentFileAsync(string path, CancellationToken cancellationToken = default);
}
