using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using wLabelDesigner.Models;
using wLabelDesigner.Services;

namespace wLabelDesigner.ViewModels;

public enum WelcomeAction
{
    CreateBlank,
    UseLayout,
    OpenExisting,
    OpenRecent
}

public sealed record WelcomeResult(
    WelcomeAction Action,
    PredefinedLabelLayout? Layout = null,
    string? Path = null);

public sealed partial class WelcomeViewModel : ObservableObject, IDisposable
{
    private readonly ILanguageService languageService;

    public WelcomeViewModel(
        ILanguageService languageService,
        IReadOnlyList<string>? recentFilePaths = null)
    {
        this.languageService = languageService;
        Layouts = new ObservableCollection<WelcomeLayoutOption>(
            PredefinedLabelLayouts.All.Select(layout => new WelcomeLayoutOption(layout, languageService)));
        RecentFiles = new ObservableCollection<WelcomeRecentFileOption>(
            (recentFilePaths ?? []).Select(path => new WelcomeRecentFileOption(path)));
        SelectedLayout = Layouts.FirstOrDefault();
        languageService.LanguageChanged += LanguageService_LanguageChanged;
    }

    public ObservableCollection<WelcomeLayoutOption> Layouts { get; }

    public ObservableCollection<WelcomeRecentFileOption> RecentFiles { get; }

    public bool HasRecentFiles => RecentFiles.Count > 0;

    public bool IsEnglish => !languageService.IsTurkish;

    public bool IsTurkish => languageService.IsTurkish;

    public WelcomeResult? Result { get; private set; }

    public event EventHandler? RequestClose;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UseSelectedLayoutCommand))]
    private WelcomeLayoutOption? selectedLayout;

    [RelayCommand]
    private void CreateBlank() => Complete(new WelcomeResult(WelcomeAction.CreateBlank));

    [RelayCommand(CanExecute = nameof(CanUseSelectedLayout))]
    private void UseSelectedLayout()
    {
        if (SelectedLayout is not null)
        {
            Complete(new WelcomeResult(WelcomeAction.UseLayout, SelectedLayout.Layout));
        }
    }

    private bool CanUseSelectedLayout() => SelectedLayout is not null;

    [RelayCommand]
    private void OpenExisting() => Complete(new WelcomeResult(WelcomeAction.OpenExisting));

    [RelayCommand]
    private void OpenRecent(WelcomeRecentFileOption? recentFile)
    {
        if (recentFile is not null)
        {
            Complete(new WelcomeResult(WelcomeAction.OpenRecent, Path: recentFile.Path));
        }
    }

    [RelayCommand]
    private void SetLanguage(string? language) => languageService.SetLanguage(language ?? "en");

    public void Dispose() => languageService.LanguageChanged -= LanguageService_LanguageChanged;

    private void Complete(WelcomeResult result)
    {
        Result = result;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void LanguageService_LanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(IsEnglish));
        OnPropertyChanged(nameof(IsTurkish));
        foreach (var layout in Layouts)
        {
            layout.RefreshLocalization();
        }
    }
}

public sealed class WelcomeRecentFileOption(string path)
{
    public string Path { get; } = path;

    public string FileName { get; } = System.IO.Path.GetFileName(path);

    public string DirectoryName { get; } = System.IO.Path.GetDirectoryName(path) ?? string.Empty;
}

public sealed class WelcomeLayoutOption(PredefinedLabelLayout layout, ILanguageService languageService) : ObservableObject
{
    public PredefinedLabelLayout Layout { get; } = layout;

    public string Name => languageService.Translate(Layout.Name);

    public string Description => languageService.Translate(Layout.Description);

    public string Size => $"{Layout.WidthMillimeters:0.#} × {Layout.HeightMillimeters:0.#} mm  ·  {Layout.PrinterDpi} DPI";

    public double PreviewWidth => Layout.WidthMillimeters >= Layout.HeightMillimeters
        ? 68
        : 86 * Layout.WidthMillimeters / Layout.HeightMillimeters;

    public double PreviewHeight => Layout.HeightMillimeters >= Layout.WidthMillimeters
        ? 86
        : 68 * Layout.HeightMillimeters / Layout.WidthMillimeters;

    public void RefreshLocalization()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Description));
    }
}
