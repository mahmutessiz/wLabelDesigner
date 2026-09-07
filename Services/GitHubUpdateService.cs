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
        return new(normalizedCurrent, normalizedVersion);
    }

    private sealed record Release(
        [property: JsonPropertyName("tag_name")] string? Tag,
        [property: JsonPropertyName("draft")] bool Draft,
        [property: JsonPropertyName("prerelease")] bool Prerelease);
}
