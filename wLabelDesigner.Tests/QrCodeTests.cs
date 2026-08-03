using wLabelDesigner.Models;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;
using Xunit;

namespace wLabelDesigner.Tests;

public sealed class QrCodeTests
{
    [Theory]
    [InlineData(QrErrorCorrectionOption.Low)]
    [InlineData(QrErrorCorrectionOption.Medium)]
    [InlineData(QrErrorCorrectionOption.Quartile)]
    [InlineData(QrErrorCorrectionOption.High)]
    public void Render_CreatesEveryErrorCorrectionLevel(QrErrorCorrectionOption errorCorrection)
    {
        var bitmap = LabelImageRenderer.Render(
            LabelElementKind.QrCode,
            "https://example.com/labels/123",
            qrErrorCorrection: errorCorrection);

        Assert.NotNull(bitmap);
        Assert.True(bitmap.PixelWidth > 0);
        Assert.Equal(bitmap.PixelWidth, bitmap.PixelHeight);
        Assert.True(bitmap.IsFrozen);
    }

    [Fact]
    public void ViewModel_RoundTripsAndNormalizesErrorCorrection()
    {
        var element = new LabelElementViewModel(new LabelElementData
        {
            Kind = LabelElementKind.QrCode,
            QrErrorCorrection = QrErrorCorrectionOption.High
        });

        Assert.Equal(QrErrorCorrectionOption.High, element.QrErrorCorrection);
        Assert.Equal(QrErrorCorrectionOption.High, element.ToData().QrErrorCorrection);

        var invalid = new LabelElementViewModel(new LabelElementData
        {
            Kind = LabelElementKind.QrCode,
            QrErrorCorrection = (QrErrorCorrectionOption)99
        });

        Assert.Equal(QrErrorCorrectionOption.Medium, invalid.QrErrorCorrection);
    }

    [Fact]
    public void ViewModel_RoundTripsAndNormalizesMargin()
    {
        var element = new LabelElementViewModel(new LabelElementData
        {
            Kind = LabelElementKind.QrCode,
            QrMarginModules = 6
        });

        Assert.Equal(6, element.QrMarginModules);
        Assert.Equal(6, element.ToData().QrMarginModules);

        element.QrMarginModules = -1;
        Assert.Equal(0, element.QrMarginModules);

        element.QrMarginModules = 20;
        Assert.Equal(16, element.QrMarginModules);
    }

    [Fact]
    public void Render_ErrorCorrectionChangesQrOutput()
    {
        var low = LabelImageRenderer.Render(
            LabelElementKind.QrCode,
            "https://example.com/labels/123",
            qrErrorCorrection: QrErrorCorrectionOption.Low);
        var high = LabelImageRenderer.Render(
            LabelElementKind.QrCode,
            "https://example.com/labels/123",
            qrErrorCorrection: QrErrorCorrectionOption.High);

        Assert.NotNull(low);
        Assert.NotNull(high);
        Assert.False(ReadPixels(low).SequenceEqual(ReadPixels(high)));
    }

    [Fact]
    public void Render_MarginAddsTheRequestedNumberOfModules()
    {
        var withoutMargin = LabelImageRenderer.Render(
            LabelElementKind.QrCode,
            "https://example.com/labels/123",
            qrMarginModules: 0);
        var withMargin = LabelImageRenderer.Render(
            LabelElementKind.QrCode,
            "https://example.com/labels/123",
            qrMarginModules: 4);

        Assert.NotNull(withoutMargin);
        Assert.NotNull(withMargin);
        Assert.Equal(withoutMargin.PixelWidth + (8 * 12), withMargin.PixelWidth);
        Assert.Equal(withMargin.PixelWidth, withMargin.PixelHeight);
    }

    private static byte[] ReadPixels(System.Windows.Media.Imaging.BitmapSource bitmap)
    {
        var stride = ((bitmap.PixelWidth * bitmap.Format.BitsPerPixel) + 7) / 8;
        var pixels = new byte[stride * bitmap.PixelHeight];
        bitmap.CopyPixels(pixels, stride, 0);
        return pixels;
    }
}
