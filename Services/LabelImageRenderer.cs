using System.IO;
using System.Windows.Media.Imaging;
using QRCoder;
using SkiaSharp;
using wLabelDesigner.Models;

namespace wLabelDesigner.Services;

public static class LabelImageRenderer
{
    private const int MaximumEmbeddedImageBytes = 25 * 1024 * 1024;

    public static BitmapSource? Render(
        LabelElementKind kind,
        string content,
        BarcodeFormatOption barcodeFormat = BarcodeFormatOption.Code128)
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
            LabelElementKind.Barcode => CreateBarcode(content, barcodeFormat),
            LabelElementKind.QrCode => CreateQrCode(content),
            LabelElementKind.Image => DecodeEmbeddedImage(content),
            _ => null
        };

        return imageBytes is null ? null : CreateBitmap(imageBytes);
    }

    private static byte[] CreateBarcode(string content, BarcodeFormatOption format)
    {
        var barcode = new BarcodeStandard.Barcode
        {
            IncludeLabel = true
        };

        using var image = barcode.Encode(
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
            width: 900,
            height: 300);
        using var encodedImage = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        return encodedImage.ToArray();
    }

    private static byte[] CreateQrCode(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(data);
        return qrCode.GetGraphic(pixelsPerModule: 12, drawQuietZones: true);
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
