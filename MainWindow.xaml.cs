using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;

namespace wLabelDesigner;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(
            new JsonLabelDocumentStore(),
            new FileDialogService(),
            new WpfLabelPrintService());
    }

    private void DesignerItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Thumb { DataContext: LabelElementViewModel element } &&
            DataContext is MainViewModel viewModel)
        {
            viewModel.SelectedElement = element;
        }
    }

    private void DesignerItem_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is not Thumb { DataContext: LabelElementViewModel element } ||
            DataContext is not MainViewModel viewModel)
        {
            return;
        }

        const double millimetersPerDeviceIndependentPixel = 25.4d / 96d;
        var maximumX = Math.Max(0, viewModel.LabelWidth - element.Width);
        var maximumY = Math.Max(0, viewModel.LabelHeight - element.Height);

        element.X = Math.Clamp(element.X + (e.HorizontalChange * millimetersPerDeviceIndependentPixel), 0, maximumX);
        element.Y = Math.Clamp(element.Y + (e.VerticalChange * millimetersPerDeviceIndependentPixel), 0, maximumY);
        viewModel.SelectedElement = element;
    }

    private void DesignerItem_ResizeDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is not Thumb { DataContext: LabelElementViewModel element } ||
            DataContext is not MainViewModel viewModel)
        {
            return;
        }

        const double millimetersPerDeviceIndependentPixel = 25.4d / 96d;
        var maximumWidth = Math.Max(1, viewModel.LabelWidth - element.X);
        var maximumHeight = Math.Max(1, viewModel.LabelHeight - element.Y);

        element.Width = Math.Clamp(
            element.Width + (e.HorizontalChange * millimetersPerDeviceIndependentPixel),
            1,
            maximumWidth);
        element.Height = Math.Clamp(
            element.Height + (e.VerticalChange * millimetersPerDeviceIndependentPixel),
            1,
            maximumHeight);
        viewModel.SelectedElement = element;
        e.Handled = true;
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}
