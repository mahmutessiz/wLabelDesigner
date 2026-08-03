using wLabelDesigner.Services;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class StartupTemplatePathResolverTests
{
    [Fact]
    public void Resolve_ReturnsFullPathForDroppedTemplate()
    {
        var relativePath = Path.Combine("templates", "shipping.fckbartndr");

        var result = StartupTemplatePathResolver.Resolve([relativePath]);

        Assert.Equal(Path.GetFullPath(relativePath), result);
    }

    [Fact]
    public void Resolve_AcceptsUppercaseExtensionAndQuotedPath()
    {
        var path = Path.Combine(Path.GetTempPath(), "Label Template.FCKBARTNDR");

        var result = StartupTemplatePathResolver.Resolve([$"\"{path}\""]);

        Assert.Equal(Path.GetFullPath(path), result);
    }

    [Fact]
    public void Resolve_IgnoresUnrelatedArguments()
    {
        var result = StartupTemplatePathResolver.Resolve(["--verbose", "notes.txt"]);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_UsesFirstTemplateWhenMultipleFilesAreDropped()
    {
        var first = Path.Combine(Path.GetTempPath(), "first.fckbartndr");
        var second = Path.Combine(Path.GetTempPath(), "second.fckbartndr");

        var result = StartupTemplatePathResolver.Resolve([first, second]);

        Assert.Equal(Path.GetFullPath(first), result);
    }
}
