using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows.Media;
using wLabelDesigner.Models;

namespace wLabelDesigner.Services;

public static class LabelPdfRenderer
{
    private static readonly byte[] BinaryHeader = [0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A];

    public static byte[] Render(LabelDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var bitmap = LabelPngRenderer.RenderBitmap(document);
        var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgr24, null, 0);
        converted.Freeze();
        var stride = checked(converted.PixelWidth * 3);
        var bgrPixels = new byte[checked(stride * converted.PixelHeight)];
        converted.CopyPixels(bgrPixels, stride, 0);
        var rgbPixels = ConvertBgrToRgb(bgrPixels);
        var compressedPixels = Compress(rgbPixels);

        var pageWidth = NormalizeDimension(document.WidthMillimeters) / 25.4d * 72d;
        var pageHeight = NormalizeDimension(document.HeightMillimeters) / 25.4d * 72d;
        var widthText = FormatNumber(pageWidth);
        var heightText = FormatNumber(pageHeight);
        var content = Encoding.ASCII.GetBytes(
            $"q\n{widthText} 0 0 {heightText} 0 0 cm\n/Im0 Do\nQ\n");

        using var output = new MemoryStream();
        WriteAscii(output, "%PDF-1.4\n");
        output.Write(BinaryHeader);
        var offsets = new long[6];
        WriteObject(output, offsets, 1, "<< /Type /Catalog /Pages 2 0 R >>");
        WriteObject(output, offsets, 2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        WriteObject(output, offsets, 3,
            $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {widthText} {heightText}] /Resources << /XObject << /Im0 4 0 R >> >> /Contents 5 0 R >>");
        WriteStreamObject(output, offsets, 4,
            $"/Type /XObject /Subtype /Image /Width {converted.PixelWidth} /Height {converted.PixelHeight} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode",
            compressedPixels);
        WriteStreamObject(output, offsets, 5, string.Empty, content);

        var crossReferenceOffset = output.Position;
        WriteAscii(output, "xref\n0 6\n0000000000 65535 f \n");
        for (var objectNumber = 1; objectNumber <= 5; objectNumber++)
        {
            WriteAscii(output, $"{offsets[objectNumber]:D10} 00000 n \n");
        }

        WriteAscii(output,
            $"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{crossReferenceOffset}\n%%EOF\n");
        return output.ToArray();
    }

    private static byte[] ConvertBgrToRgb(byte[] source)
    {
        var result = new byte[source.Length];
        for (var index = 0; index < source.Length; index += 3)
        {
            result[index] = source[index + 2];
            result[index + 1] = source[index + 1];
            result[index + 2] = source[index];
        }

        return result;
    }

    private static byte[] Compress(byte[] bytes)
    {
        using var output = new MemoryStream();
        using (var compression = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            compression.Write(bytes);
        }

        return output.ToArray();
    }

    private static void WriteObject(Stream output, long[] offsets, int objectNumber, string body)
    {
        offsets[objectNumber] = output.Position;
        WriteAscii(output, $"{objectNumber} 0 obj\n{body}\nendobj\n");
    }

    private static void WriteStreamObject(
        Stream output,
        long[] offsets,
        int objectNumber,
        string dictionaryEntries,
        byte[] content)
    {
        offsets[objectNumber] = output.Position;
        var entries = string.IsNullOrEmpty(dictionaryEntries) ? string.Empty : $" {dictionaryEntries}";
        WriteAscii(output, $"{objectNumber} 0 obj\n<< /Length {content.Length}{entries} >>\nstream\n");
        output.Write(content);
        WriteAscii(output, "\nendstream\nendobj\n");
    }

    private static void WriteAscii(Stream output, string value) =>
        output.Write(Encoding.ASCII.GetBytes(value));

    private static string FormatNumber(double value) =>
        value.ToString("0.#####", CultureInfo.InvariantCulture);

    private static double NormalizeDimension(double value) =>
        double.IsFinite(value) ? Math.Clamp(value, 1, 1000) : 1;
}
