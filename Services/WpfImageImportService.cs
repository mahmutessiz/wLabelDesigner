using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace wLabelDesigner.Services;

public sealed class WpfImageImportService : IImageImportService
{
    private const long MaximumImageBytes = 25 * 1024 * 1024;
    private const string ImageFilter =
        "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|All files (*.*)|*.*";

    public ImportedImage? ImportImage()
    {
        var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            Filter = ImageFilter,
            Title = "Choose an image or logo"
        };

        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        var file = new FileInfo(dialog.FileName);
        if (file.Length > MaximumImageBytes)
        {
            throw new InvalidDataException("The selected image is larger than 25 MB.");
        }

        var bytes = File.ReadAllBytes(file.FullName);
        BitmapFrame? frame;
        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            var decoder = BitmapDecoder.Create(
                stream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);
            frame = decoder.Frames.FirstOrDefault();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or NotSupportedException or IOException)
        {
            throw new InvalidDataException("The selected file is not a supported image.", exception);
        }

        if (frame is null || frame.PixelWidth <= 0 || frame.PixelHeight <= 0)
        {
            throw new InvalidDataException("The selected file does not contain a readable image.");
        }

        var mediaType = Path.GetExtension(file.Name).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".bmp" => "image/bmp",
            ".gif" => "image/gif",
            ".tif" or ".tiff" => "image/tiff",
            _ => throw new InvalidDataException("The selected image format is not supported.")
        };

        return new ImportedImage(
            $"data:{mediaType};base64,{Convert.ToBase64String(bytes)}",
            frame.PixelWidth,
            frame.PixelHeight);
    }
}
