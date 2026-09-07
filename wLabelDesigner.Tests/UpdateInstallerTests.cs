using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class UpdateInstallerTests
{
    [Theory]
    [InlineData("update", true, true, "A fresh update is ready")]
    [InlineData("current", false, true, "You're up to date")]
    [InlineData("missing", false, true, "No updates just yet")]
    [InlineData("error", false, false, "Couldn't check for updates")]
    public void Dialog_PresentsActionsForEachState(string state, bool showDownload, bool showVersions, string title)
    {
        var result = new UpdateCheckResult(new Version(1, 1, 0, 0),
            state == "missing" ? null : new Version(state == "update" ? 2 : 1, 1, 0, 0), Installer);
        var viewModel = new UpdateViewModel(result, new StubInstallerService(), false,
            state == "error" ? "Please try again later." : null);

        Assert.Equal(title, viewModel.Title);
        Assert.Equal(showDownload, viewModel.ShowDownload);
        Assert.Equal(showVersions, viewModel.ShowVersions);
        Assert.Equal(showDownload, viewModel.DownloadCommand.CanExecute(null));
        Assert.Equal("1.1.0", viewModel.CurrentVersionText);
        if (state == "error") Assert.Equal("Please try again later.", viewModel.Description);
    }

    [Fact]
    public void Dialog_LocalizesCurrentState()
    {
        var viewModel = new UpdateViewModel(new(new Version(1, 1), new Version(1, 1)), new StubInstallerService(), true);
        Assert.Equal("Uygulamanız güncel", viewModel.Title);
        Assert.Equal("Tamam", viewModel.CloseLabel);
        Assert.Equal("Yüklü sürüm", viewModel.CurrentVersionLabel);
    }

    private static readonly byte[] Content = [1, 2, 3, 4];
    private static UpdateInstaller Installer => new(
        new Uri("https://github.com/mahmutessiz/wLabelDesigner/releases/download/v2.0.0/wLabelDesigner-2.0.0-win-x64-setup.exe"),
        Content.Length, Convert.ToHexString(SHA256.HashData(Content)));

    [Theory]
    [InlineData("valid")]
    [InlineData("hash")]
    [InlineData("short")]
    [InlineData("long")]
    [InlineData("http")]
    [InlineData("cancel")]
    public async Task Download_VerifiesBytesAndRemovesIncompleteFiles(string scenario)
    {
        var directory = Path.Combine(Path.GetTempPath(), "wld-tests-" + Guid.NewGuid().ToString("N"));
        using var client = new HttpClient(new DownloadHandler(scenario == "http" ? HttpStatusCode.NotFound : HttpStatusCode.OK));
        var service = new UpdateInstallerService(client, directory, _ => throw new InvalidOperationException("Must not install during download"));
        var installer = scenario switch
        {
            "hash" => Installer with { Sha256 = new string('0', 64) },
            "short" => Installer with { Size = 5 },
            "long" => Installer with { Size = 3 },
            _ => Installer
        };
        using var cancellation = new CancellationTokenSource();
        if (scenario == "cancel") cancellation.Cancel();
        try
        {
            var progress = new CaptureProgress();
            if (scenario == "valid")
            {
                var path = await service.DownloadAsync(installer, progress, cancellation.Token);
                Assert.Equal(Content, await File.ReadAllBytesAsync(path));
                Assert.Equal(100, progress.Value);
            }
            else
            {
                await Assert.ThrowsAnyAsync<Exception>(() => service.DownloadAsync(installer, progress, cancellation.Token));
                Assert.Empty(Directory.GetFiles(directory));
            }
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task Download_RejectsUntrustedUrl()
    {
        using var client = new HttpClient(new DownloadHandler(HttpStatusCode.OK));
        var service = new UpdateInstallerService(client, "unused", _ => Task.FromResult(true));
        await Assert.ThrowsAsync<InvalidDataException>(() => service.DownloadAsync(
            Installer with { DownloadUri = new Uri("https://example.com/setup.exe") }, new CaptureProgress(), default));
    }

    [Fact]
    public async Task DownloadCommand_CancelledSavePromptKeepsDownloadForRetry()
    {
        var service = new StubInstallerService();
        var viewModel = new UpdateViewModel(new(new Version(1, 0), new Version(2, 0), Installer), service, false);
        await viewModel.DownloadCommand.ExecuteAsync(null);
        Assert.Equal("Install Update", viewModel.DownloadLabel);
        Assert.Contains("Installation cancelled", viewModel.Status);
        await viewModel.DownloadCommand.ExecuteAsync(null);
        Assert.Equal(1, service.Downloads);
        Assert.Equal(2, service.Installs);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task DownloadCommand_FailureNeverStartsInstallerAndAllowsRetry()
    {
        var service = new StubInstallerService { Fail = true };
        var viewModel = new UpdateViewModel(new(new Version(1, 0), new Version(2, 0), Installer), service, false);
        await viewModel.DownloadCommand.ExecuteAsync(null);
        Assert.Equal(0, service.Installs);
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.DownloadCommand.CanExecute(null));
    }

    [Fact]
    public void DownloadCommand_DisabledWithoutInstaller()
    {
        var viewModel = new UpdateViewModel(new(new Version(1, 0), new Version(2, 0)), new StubInstallerService(), false);
        Assert.False(viewModel.DownloadCommand.CanExecute(null));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Check_SelectsOnlyVerifiedMatchingInstaller(bool validDigest)
    {
        var digest = validDigest ? "sha256:" + Installer.Sha256 : "";
        var json = $$"""
            {"tag_name":"v2.0.0","assets":[
            {"name":"wLabelDesigner.exe","size":4,"browser_download_url":"{{Installer.DownloadUri}}","digest":"{{digest}}"},
            {"name":"wLabelDesigner-2.0.0-win-x64-setup.exe","size":4,"browser_download_url":"{{Installer.DownloadUri}}","digest":"{{digest}}"}]}
            """;
        using var client = new HttpClient(new JsonHandler(json));
        var result = await new GitHubUpdateService(client, new Version(1, 0)).CheckAsync();
        Assert.Equal(validDigest, result.Installer is not null);
        if (validDigest) Assert.Equal(Installer, result.Installer);
    }

    private sealed class CaptureProgress : IProgress<double>
    {
        public double Value { get; private set; }
        public void Report(double value) => Value = value;
    }

    private sealed class DownloadHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(status) { Content = new ByteArrayContent(Content) });
        }
    }

    private sealed class JsonHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
    }

    private sealed class StubInstallerService : IUpdateInstallerService
    {
        public bool Fail { get; init; }
        public int Downloads { get; private set; }
        public int Installs { get; private set; }
        public Task<string> DownloadAsync(UpdateInstaller installer, IProgress<double> progress, CancellationToken cancellationToken)
        {
            Downloads++;
            return Fail ? Task.FromException<string>(new IOException("Failed")) : Task.FromResult("verified.exe");
        }
        public Task<bool> InstallAsync(string path)
        {
            Installs++;
            return Task.FromResult(false);
        }
    }
}
