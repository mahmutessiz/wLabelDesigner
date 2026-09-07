using System.IO;
using System.Net.Http;
using System.Security.Cryptography;

namespace wLabelDesigner.Services;

public sealed class UpdateInstallerService(
    HttpClient client, string downloadDirectory, Func<string, Task<bool>> install) : IUpdateInstallerService
{
    public async Task<string> DownloadAsync(UpdateInstaller installer, IProgress<double> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(installer);
        if (installer.DownloadUri.Scheme != "https" || installer.DownloadUri.Host != "github.com" ||
            !installer.DownloadUri.AbsolutePath.StartsWith("/mahmutessiz/wLabelDesigner/releases/download/", StringComparison.Ordinal) ||
            installer.Size <= 0 || installer.Sha256.Length != 64 || !installer.Sha256.All(Uri.IsHexDigit))
        {
            throw new InvalidDataException("Invalid update installer metadata.");
        }

        Directory.CreateDirectory(downloadDirectory);
        var path = Path.Combine(downloadDirectory, $"update-{Guid.NewGuid():N}.exe");
        var complete = false;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromMinutes(15));
            var token = timeout.Token;
            using var response = await client.GetAsync(installer.DownloadUri, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            await using var input = await response.Content.ReadAsStreamAsync(token);
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[81920];
            long received = 0;
            int count;
            while ((count = await input.ReadAsync(buffer, token)) != 0)
            {
                received += count;
                if (received > installer.Size)
                {
                    throw new InvalidDataException("The update download has an unexpected size.");
                }
                hash.AppendData(buffer, 0, count);
                await output.WriteAsync(buffer.AsMemory(0, count), token);
                progress.Report(received * 100d / installer.Size);
            }
            if (received != installer.Size ||
                !Convert.ToHexString(hash.GetHashAndReset()).Equals(installer.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The update download failed verification. Please try again.");
            }
            await output.FlushAsync(token);
            complete = true;
            return path;
        }
        finally
        {
            if (!complete && File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    public Task<bool> InstallAsync(string path) => install(path);
}
