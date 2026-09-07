using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace wLabelDesigner.Services;

public sealed class GitHubUpdateService(HttpClient httpClient, Version currentVersion) : IUpdateService
{
    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            "https://api.github.com/repos/mahmutessiz/wLabelDesigner/releases/latest");
        request.Headers.UserAgent.ParseAdd("wLabelDesigner/" + currentVersion);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new(currentVersion, null);
        }

        response.EnsureSuccessStatusCode();
        var release = await response.Content.ReadFromJsonAsync<Release>(cancellationToken);
        if (release is null || release.Draft || release.Prerelease ||
            !Version.TryParse(release.Tag?.TrimStart('v', 'V'), out var version))
        {
            throw new JsonException("The latest release does not contain a supported stable version tag.");
        }

        var normalizedVersion = new Version(version.Major, version.Minor,
            Math.Max(0, version.Build), Math.Max(0, version.Revision));
        var normalizedCurrent = new Version(currentVersion.Major, currentVersion.Minor,
            Math.Max(0, currentVersion.Build), Math.Max(0, currentVersion.Revision));
        var expectedName = $"wLabelDesigner-{release.Tag!.TrimStart('v', 'V')}-win-x64-setup.exe";
        var asset = release.Assets?.FirstOrDefault(asset => asset.Name == expectedName);
        UpdateInstaller? installer = null;
        if (asset is { Size: > 0, Digest: not null } &&
            asset.Digest.StartsWith("sha256:", StringComparison.Ordinal) &&
            asset.Digest.Length == 71 && asset.Digest[7..].All(Uri.IsHexDigit) &&
            Uri.TryCreate(asset.Url, UriKind.Absolute, out var uri) &&
            uri.Scheme == "https" && uri.Host == "github.com" &&
            uri.AbsolutePath.StartsWith("/mahmutessiz/wLabelDesigner/releases/download/", StringComparison.Ordinal))
        {
            installer = new(uri, asset.Size, asset.Digest[7..]);
        }
        return new(normalizedCurrent, normalizedVersion, installer);
    }

    private sealed record Release(
        [property: JsonPropertyName("tag_name")] string? Tag,
        [property: JsonPropertyName("draft")] bool Draft,
        [property: JsonPropertyName("prerelease")] bool Prerelease,
        [property: JsonPropertyName("assets")] Asset[]? Assets);

    private sealed record Asset(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("browser_download_url")] string? Url,
        [property: JsonPropertyName("size")] long Size,
        [property: JsonPropertyName("digest")] string? Digest);
}
