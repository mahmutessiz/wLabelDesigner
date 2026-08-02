using System.Windows;

namespace wLabelDesigner.Services;

public sealed class WpfUnsavedChangesPromptService : IUnsavedChangesPromptService
{
    public UnsavedChangesChoice ConfirmSaveChanges(string documentName)
    {
        var isTurkish = WpfLanguageService.Instance.IsTurkish;
        var result = MessageBox.Show(
            Application.Current.MainWindow,
            isTurkish
                ? $"Devam etmeden önce “{documentName}” belgesindeki değişiklikler kaydedilsin mi?\n\nEvet: Kaydet   Hayır: Kaydetme   İptal: Düzenlemeye devam et"
                : $"Save changes to “{documentName}” before continuing?\n\nYes: Save   No: Don’t save   Cancel: Keep editing",
            isTurkish ? "Kaydedilmemiş değişiklikler" : "Unsaved changes",
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
