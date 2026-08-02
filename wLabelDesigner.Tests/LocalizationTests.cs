using wLabelDesigner.Services;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class LocalizationTests
{
    [Theory]
    [InlineData("_File", "_Dosya")]
    [InlineData("Print preview", "Baskı önizleme")]
    [InlineData("Lock position", "Konumu kilitle")]
    [InlineData("Image", "Görsel")]
    public void TranslateForLanguage_ReturnsTurkishText(string english, string expected)
    {
        Assert.Equal(expected, WpfLanguageService.TranslateForLanguage(english, "tr"));
        Assert.Equal(english, WpfLanguageService.TranslateForLanguage(english, "en"));
    }
}
