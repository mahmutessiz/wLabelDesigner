using System.IO;
using System.Text.Json;

namespace wLabelDesigner.Services;

public sealed class JsonRecentFilesService : IRecentFilesService
{
    public const int MaximumRecentFiles = 10;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string storagePath;
    private readonly SemaphoreSlim accessLock = new(1, 1);

    public JsonRecentFilesService(string? storageDirectory = null)
    {
        var directory = storageDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "wLabelDesigner");
        storagePath = Path.Combine(directory, "recent-files.json");
    }

    public async Task<IReadOnlyList<string>> GetRecentFilesAsync(
        CancellationToken cancellationToken = default)
    {
        await accessLock.WaitAsync(cancellationToken);
        try
        {
            var storedPaths = await LoadCoreAsync(cancellationToken);
            var existingPaths = NormalizeExistingPaths(storedPaths);
            if (!storedPaths.SequenceEqual(existingPaths, StringComparer.OrdinalIgnoreCase))
            {
                await SaveCoreAsync(existingPaths, cancellationToken);
            }

            return existingPaths;
        }
        finally
        {
            accessLock.Release();
        }
    }

    public async Task AddRecentFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var normalizedPath = Path.GetFullPath(path);

        await accessLock.WaitAsync(cancellationToken);
        try
        {
            var existingPaths = NormalizeExistingPaths(await LoadCoreAsync(cancellationToken));
            var updatedPaths = existingPaths
                .Where(existing => !string.Equals(existing, normalizedPath, StringComparison.OrdinalIgnoreCase))
                .Prepend(normalizedPath)
                .Take(MaximumRecentFiles)
                .ToArray();
            await SaveCoreAsync(updatedPaths, cancellationToken);
        }
        finally
        {
            accessLock.Release();
        }
    }

    public async Task RemoveRecentFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var normalizedPath = Path.GetFullPath(path);

        await accessLock.WaitAsync(cancellationToken);
        try
        {
            var updatedPaths = (await LoadCoreAsync(cancellationToken))
                .Where(existing => !string.Equals(existing, normalizedPath, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            await SaveCoreAsync(updatedPaths, cancellationToken);
        }
        finally
        {
            accessLock.Release();
        }
    }

    private async Task<string[]> LoadCoreAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(storagePath))
        {
            return [];
        }

        try
        {
            await using var stream = new FileStream(
                storagePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            return await JsonSerializer.DeserializeAsync<string[]>(stream, SerializerOptions, cancellationToken) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task SaveCoreAsync(IReadOnlyList<string> paths, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(storagePath)
            ?? throw new InvalidOperationException("The recent-files storage path has no directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = Path.Combine(directory, $"recent-files-{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(stream, paths, SerializerOptions, cancellationToken);
            }

            File.Move(temporaryPath, storagePath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    private static string[] NormalizeExistingPaths(IEnumerable<string> paths) => paths
        .Select(TryNormalizePath)
        .Where(path => path is not null && File.Exists(path))
        .Cast<string>()
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(MaximumRecentFiles)
        .ToArray();

    private static string? TryNormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }
}
