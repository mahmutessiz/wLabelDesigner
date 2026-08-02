using System.Windows;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;

namespace wLabelDesigner;

public partial class WelcomeWindow : Window
{
    private readonly WpfLanguageService languageService = WpfLanguageService.Instance;
    private readonly WelcomeViewModel viewModel;

    public WelcomeWindow(WelcomeViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        DataContext = viewModel;
        viewModel.RequestClose += ViewModel_RequestClose;
        languageService.LanguageChanged += LanguageService_LanguageChanged;
        Loaded += (_, _) => languageService.Apply(this);
        Closed += WelcomeWindow_Closed;
    }

    private void ViewModel_RequestClose(object? sender, EventArgs e) => DialogResult = true;

    private void LanguageService_LanguageChanged(object? sender, EventArgs e) => languageService.Apply(this);

    private void WelcomeWindow_Closed(object? sender, EventArgs e)
    {
        viewModel.RequestClose -= ViewModel_RequestClose;
        languageService.LanguageChanged -= LanguageService_LanguageChanged;
    }
}
