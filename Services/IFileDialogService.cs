namespace wLabelDesigner.Services;

public interface IFileDialogService
{
    string? ChooseTemplateToOpen();

    string? ChooseTemplateToSave(string suggestedFileName);
}
