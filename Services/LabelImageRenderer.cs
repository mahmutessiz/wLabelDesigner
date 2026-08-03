using System.IO;
using System.Windows.Media.Imaging;
using QRCoder;
using SkiaSharp;
using wLabelDesigner.Models;

namespace wLabelDesigner.Services;

public static class LabelImageRenderer
{
    private const int MaximumEmbeddedImageBytes = 25 * 1024 * 1024;
    private const int BarcodePixelWidth = 900;
    private const int BarcodePixelHeight = 300;
    private const int QrPixelsPerModule = 12;

    public static BitmapSource? Render(
        LabelElementKind kind,
        string content,
        BarcodeFormatOption barcodeFormat = BarcodeFormatOption.Code128,
        bool isBarcodeTextVisible = true,
        double barcodeQuietZoneMillimeters = 2,
        double barcodeWidthMillimeters = 50,
        QrErrorCorrectionOption qrErrorCorrection = QrErrorCorrectionOption.Medium,
        int qrMarginModules = 4)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        if (kind == LabelElementKind.Barcode && !BarcodeContentValidator.IsValid(barcodeFormat, content))
        {
            return null;
        }

        var imageBytes = kind switch
        {
            LabelElementKind.Barcode => CreateBarcode(
                content,
                barcodeFormat,
                isBarcodeTextVisible,
                barcodeQuietZoneMillimeters,
                barcodeWidthMillimeters),
            LabelElementKind.QrCode => CreateQrCode(content, qrErrorCorrection, qrMarginModules),
            LabelElementKind.Image => DecodeEmbeddedImage(content),
            _ => null
        };

        return imageBytes is null ? null : CreateBitmap(imageBytes);
    }

    private static byte[] CreateBarcode(
        string content,
        BarcodeFormatOption format,
        bool isBarcodeTextVisible,
        double quietZoneMillimeters,
        double barcodeWidthMillimeters)
    {
        var quietZonePixels = CalculateQuietZonePixels(quietZoneMillimeters, barcodeWidthMillimeters);
        var contentWidth = BarcodePixelWidth - (quietZonePixels * 2);
        var barcode = new BarcodeStandard.Barcode
        {
            IncludeLabel = isBarcodeTextVisible
        };

        using var barcodeImage = barcode.Encode(
            format switch
            {
                BarcodeFormatOption.Code39 => BarcodeStandard.Type.Code39,
                BarcodeFormatOption.Ean8 => BarcodeStandard.Type.Ean8,
                BarcodeFormatOption.Ean13 => BarcodeStandard.Type.Ean13,
                BarcodeFormatOption.UpcA => BarcodeStandard.Type.UpcA,
                BarcodeFormatOption.Itf14 => BarcodeStandard.Type.Itf14,
                _ => BarcodeStandard.Type.Code128
            },
            content,
            SKColors.Black,
            SKColors.White,
            width: contentWidth,
            height: BarcodePixelHeight);
        using var surface = SKSurface.Create(new SKImageInfo(BarcodePixelWidth, BarcodePixelHeight));
        surface.Canvas.Clear(SKColors.White);
        surface.Canvas.DrawImage(barcodeImage, quietZonePixels, 0);
        using var image = surface.Snapshot();
        using var encodedImage = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        return encodedImage.ToArray();
    }

    private static int CalculateQuietZonePixels(double quietZoneMillimeters, double barcodeWidthMillimeters)
    {
        var width = double.IsFinite(barcodeWidthMillimeters) && barcodeWidthMillimeters > 0
            ? barcodeWidthMillimeters
            : 50;
        var quietZone = double.IsFinite(quietZoneMillimeters)
            ? Math.Clamp(quietZoneMillimeters, 0, Math.Min(25, width * 0.45))
            : 2;
        return (int)Math.Round((quietZone / width) * BarcodePixelWidth);
    }

    private static byte[] CreateQrCode(
        string content,
        QrErrorCorrectionOption errorCorrection,
        int marginModules)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(
            content,
            errorCorrection switch
            {
                QrErrorCorrectionOption.Low => QRCodeGenerator.ECCLevel.L,
                QrErrorCorrectionOption.Quartile => QRCodeGenerator.ECCLevel.Q,
                QrErrorCorrectionOption.High => QRCodeGenerator.ECCLevel.H,
                _ => QRCodeGenerator.ECCLevel.M
            });
        using var qrCode = new PngByteQRCode(data);
        var qrBytes = qrCode.GetGraphic(pixelsPerModule: QrPixelsPerModule, drawQuietZones: false);
        var marginPixels = Math.Clamp(marginModules, 0, 16) * QrPixelsPerModule;
        if (marginPixels == 0)
        {
            return qrBytes;
        }

        using var qrBitmap = SKBitmap.Decode(qrBytes) ??
            throw new InvalidOperationException("Generated QR code could not be decoded.");
        var imageSize = qrBitmap.Width + (marginPixels * 2);
        using var surface = SKSurface.Create(new SKImageInfo(imageSize, imageSize));
        surface.Canvas.Clear(SKColors.White);
        surface.Canvas.DrawBitmap(qrBitmap, marginPixels, marginPixels);
        using var image = surface.Snapshot();
        using var encodedImage = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        return encodedImage.ToArray();
    }

    private static byte[]? DecodeEmbeddedImage(string content)
    {
        const string marker = ";base64,";
        if (!content.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var markerIndex = content.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return null;
        }

        var encoded = content[(markerIndex + marker.Length)..];
        if (encoded.Length > ((MaximumEmbeddedImageBytes + 2L) / 3L) * 4L)
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(encoded);
            return bytes.Length <= MaximumEmbeddedImageBytes ? bytes : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static BitmapImage? CreateBitmap(byte[] imageBytes)
    {
        try
        {
            using var stream = new MemoryStream(imageBytes, writable: false);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NotSupportedException or IOException)
        {
            return null;
        }
    }
}
