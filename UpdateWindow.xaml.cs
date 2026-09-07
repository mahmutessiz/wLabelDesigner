using System.ComponentModel;
using System.Windows;
using wLabelDesigner.ViewModels;

namespace wLabelDesigner;

public partial class UpdateWindow : Window
{
    public UpdateWindow(UpdateViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(CancelEventArgs e)
    {
        if (DataContext is UpdateViewModel { IsBusy: true } viewModel)
        {
            viewModel.DownloadCommand.Cancel();
            e.Cancel = true;
        }
        base.OnClosing(e);
    }
}
