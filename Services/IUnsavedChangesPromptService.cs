namespace wLabelDesigner.Services;

public interface IUnsavedChangesPromptService
{
    UnsavedChangesChoice ConfirmSaveChanges(string documentName);
}

public enum UnsavedChangesChoice
{
    Save,
    Discard,
    Cancel
}
