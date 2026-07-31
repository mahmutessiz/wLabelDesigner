using Microsoft.Win32;

namespace wLabelDesigner.Services;

public sealed class FileDialogService : IFileDialogService
{
    private const string TemplateFilter = "FckBarTender label (*.fckbartndr)|*.fckbartndr|JSON files (*.json)|*.json|All files (*.*)|*.*";

    public string? ChooseTemplateToOpen()
    {
        var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            DefaultExt = ".fckbartndr",
            Filter = TemplateFilter,
            Title = "Open label template"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? ChooseTemplateToSave(string suggestedFileName)
    {
        var dialog = new SaveFileDialog
        {
            AddExtension = true,
            DefaultExt = ".fckbartndr",
            FileName = suggestedFileName,
            Filter = TemplateFilter,
            OverwritePrompt = true,
            Title = "Save label template"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
