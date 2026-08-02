using System.Windows;

namespace wLabelDesigner.Services;

public sealed class WpfUnsavedChangesPromptService : IUnsavedChangesPromptService
{
    public UnsavedChangesChoice ConfirmSaveChanges(string documentName)
    {
        var result = MessageBox.Show(
            Application.Current.MainWindow,
            $"Save changes to “{documentName}” before continuing?\n\nYes: Save   No: Don’t save   Cancel: Keep editing",
            "Unsaved changes",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning,
            MessageBoxResult.Yes);

        return result switch
        {
            MessageBoxResult.Yes => UnsavedChangesChoice.Save,
            MessageBoxResult.No => UnsavedChangesChoice.Discard,
            _ => UnsavedChangesChoice.Cancel
        };
    }
}
