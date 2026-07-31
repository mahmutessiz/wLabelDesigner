using System.IO;
using System.Windows.Media.Imaging;
using QRCoder;
using SkiaSharp;
using wLabelDesigner.Models;

namespace wLabelDesigner.Services;

public static class LabelImageRenderer
{
    public static BitmapSource? Render(LabelElementKind kind, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var imageBytes = kind switch
        {
            LabelElementKind.Barcode => CreateBarcode(content),
            LabelElementKind.QrCode => CreateQrCode(content),
            _ => null
        };

        return imageBytes is null ? null : CreateBitmap(imageBytes);
    }

    private static byte[] CreateBarcode(string content)
    {
        var barcode = new BarcodeStandard.Barcode
        {
            IncludeLabel = true
        };

        using var image = barcode.Encode(
            BarcodeStandard.Type.Code128,
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

    private static BitmapImage CreateBitmap(byte[] imageBytes)
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
}
