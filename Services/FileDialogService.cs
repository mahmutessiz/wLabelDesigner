using Microsoft.Win32;

namespace wLabelDesigner.Services;

public sealed class FileDialogService : IFileDialogService
{
    private const string TemplateFilter = "wLabelDesigner label (*.wld)|*.wld|JSON files (*.json)|*.json|All files (*.*)|*.*";

    public string? ChooseTemplateToOpen()
    {
        var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            DefaultExt = ".wld",
            Filter = TemplateFilter,
            Title = WpfLanguageService.Instance.IsTurkish ? "Etiket şablonu aç" : "Open label template"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? ChooseTemplateToSave(string suggestedFileName)
    {
        var dialog = new SaveFileDialog
        {
            AddExtension = true,
            DefaultExt = ".wld",
            FileName = suggestedFileName,
            Filter = TemplateFilter,
            OverwritePrompt = true,
            Title = WpfLanguageService.Instance.IsTurkish ? "Etiket şablonunu kaydet" : "Save label template"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
