namespace wLabelDesigner.Services;

public interface ILanguageService
{
    string CurrentLanguage { get; }

    bool IsTurkish { get; }

    event EventHandler? LanguageChanged;

    void SetLanguage(string language);

    string Translate(string text);
}
