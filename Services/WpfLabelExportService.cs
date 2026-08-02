using System.IO;
using Microsoft.Win32;
using wLabelDesigner.Models;

namespace wLabelDesigner.Services;

public sealed class WpfLabelExportService : ILabelExportService
{
    public async Task<string?> ExportPngAsync(
        LabelDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        var dialog = new SaveFileDialog
        {
            AddExtension = true,
            DefaultExt = ".png",
            FileName = MakeSafeFileName(document.Name),
            Filter = "PNG image (*.png)|*.png",
            OverwritePrompt = true,
            Title = "Export label as PNG"
        };

        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        var png = LabelPngRenderer.Render(document);
        await File.WriteAllBytesAsync(dialog.FileName, png, cancellationToken);
        return dialog.FileName;
    }

    private static string MakeSafeFileName(string name)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var safeName = new string((name ?? string.Empty)
            .Where(character => !invalidCharacters.Contains(character))
            .ToArray())
            .Trim();
        return string.IsNullOrEmpty(safeName) ? "Untitled label" : safeName;
    }
}
