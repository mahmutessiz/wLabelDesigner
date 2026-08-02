namespace wLabelDesigner.Services;

public interface IImageImportService
{
    ImportedImage? ImportImage();
}

public sealed record ImportedImage(string DataUri, int PixelWidth, int PixelHeight);
