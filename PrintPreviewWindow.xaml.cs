using System.Windows;
using wLabelDesigner.ViewModels;
using wLabelDesigner.Services;

namespace wLabelDesigner;

public partial class PrintPreviewWindow : Window
{
    public PrintPreviewWindow(PrintPreviewViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) => WpfLanguageService.Instance.Apply(this);
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is PrintPreviewViewModel { CanPrint: true })
        {
            DialogResult = true;
        }
    }
}
