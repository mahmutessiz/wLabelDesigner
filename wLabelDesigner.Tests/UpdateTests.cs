using System.Net;
using System.Net.Http;
using System.Text.Json;
using wLabelDesigner.Services;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class UpdateTests
{
    [Theory]
    [InlineData("v1.1.0", true)]
    [InlineData("1.0.0", false)]
    [InlineData("v1.0", false)]
    [InlineData("v0.9.9", false)]
    [InlineData("v1.10.0", true)]
    public async Task Check_ComparesNumericVersions(string tag, bool expected)
    {
        using var client = CreateClient(HttpStatusCode.OK, $$"""{"tag_name":"{{tag}}","draft":false,"prerelease":false}""");
        var result = await new GitHubUpdateService(client, new Version(1, 0, 0, 0)).CheckAsync();
        Assert.Equal(expected, result.UpdateAvailable);
    }

    [Fact]
    public async Task Check_WhenNoRelease_ReturnsNoVersion()
    {
        using var client = CreateClient(HttpStatusCode.NotFound, "");
        var result = await new GitHubUpdateService(client, new Version(1, 0)).CheckAsync();
        Assert.Null(result.LatestVersion);
        Assert.False(result.UpdateAvailable);
    }

    [Theory]
    [InlineData("{\"tag_name\":\"banana\"}")]
    [InlineData("{\"tag_name\":\"v2.0.0\",\"prerelease\":true}")]
    [InlineData("{\"tag_name\":\"v2.0.0\",\"draft\":true}")]
    [InlineData("{}")]
    [InlineData("not json")]
    public async Task Check_RejectsInvalidRelease(string json)
    {
        using var client = CreateClient(HttpStatusCode.OK, json);
        await Assert.ThrowsAsync<JsonException>(() =>
            new GitHubUpdateService(client, new Version(1, 0)).CheckAsync());
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Check_ReportsHttpFailures(HttpStatusCode status)
    {
        using var client = CreateClient(status, "");
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            new GitHubUpdateService(client, new Version(1, 0)).CheckAsync());
    }

    [Fact]
    public async Task Check_PropagatesCancellation()
    {
        using var client = CreateClient(HttpStatusCode.OK, "{}");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new GitHubUpdateService(client, new Version(1, 0)).CheckAsync(cancellation.Token));
    }

    private static HttpClient CreateClient(HttpStatusCode status, string json) => new(new Handler(status, json));

    private sealed class Handler(HttpStatusCode status, string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Equal("https://api.github.com/repos/mahmutessiz/wLabelDesigner/releases/latest", request.RequestUri!.AbsoluteUri);
            Assert.NotEmpty(request.Headers.UserAgent);
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json) });
        }
    }
}
