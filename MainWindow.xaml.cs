using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using wLabelDesigner.Models;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;

namespace wLabelDesigner;

public partial class MainWindow : Window
{
    private Point toolboxDragStart;
    private LabelElementViewModel? inlineEditingElement;
    private string? inlineEditOriginalContent;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(
            new JsonLabelDocumentStore(),
            new FileDialogService(),
            new WpfLabelPrintService(),
            new WpfElementClipboard());
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.FocusedElement is TextBoxBase or ComboBox || DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var controlPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        var shiftPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

        if (e.Key == Key.Tab && viewModel.Elements.Count > 0)
        {
            SelectAdjacentElement(viewModel, shiftPressed ? -1 : 1);
            e.Handled = true;
            return;
        }

        System.Windows.Input.ICommand? command = null;
        object? parameter = null;

        if (controlPressed)
        {
            command = e.Key switch
            {
                Key.C => viewModel.CopyCommand,
                Key.X => viewModel.CutCommand,
                Key.V => viewModel.PasteCommand,
                Key.D => viewModel.DuplicateCommand,
                _ => null
            };
        }
        else if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down)
        {
            command = viewModel.NudgeCommand;
            parameter = $"{e.Key}{(shiftPressed ? ":Large" : string.Empty)}";
        }
        else if (e.Key == Key.Delete)
        {
            command = viewModel.DeleteSelectedCommand;
        }

        if (command?.CanExecute(parameter) == true)
        {
            command.Execute(parameter);
            e.Handled = true;
        }
    }

    private static void SelectAdjacentElement(MainViewModel viewModel, int direction)
    {
        var currentIndex = viewModel.SelectedElement is null
            ? (direction > 0 ? -1 : 0)
            : viewModel.Elements.IndexOf(viewModel.SelectedElement);
        var nextIndex = (currentIndex + direction + viewModel.Elements.Count) % viewModel.Elements.Count;
        viewModel.SelectedElement = viewModel.Elements[nextIndex];
        viewModel.StatusMessage = $"Selected {viewModel.SelectedElement.DisplayName}";
    }

    private void DesignerCanvas_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            if (Keyboard.FocusedElement is TextBoxBase)
            {
                return;
            }

            if (DataContext is MainViewModel { SelectedElement: not null } viewModel &&
                DesignerCanvas.ItemContainerGenerator.ContainerFromItem(viewModel.SelectedElement) is ListBoxItem container)
            {
                container.Focus();
            }
            else
            {
                DesignerCanvas.Focus();
            }
        });
    }

    private void ToolboxItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        toolboxDragStart = e.GetPosition(this);

    private void ToolboxItem_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed ||
            sender is not Button { Tag: LabelElementKind kind })
        {
            return;
        }

        var currentPosition = e.GetPosition(this);
        if (Math.Abs(currentPosition.X - toolboxDragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(currentPosition.Y - toolboxDragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var data = new DataObject(typeof(LabelElementKind), kind);
        DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Copy);
    }

    private void DesignerCanvas_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(LabelElementKind)) ||
            e.Data.GetData(typeof(LabelElementKind)) is not LabelElementKind kind ||
            DataContext is not MainViewModel viewModel)
        {
            return;
        }

        const double millimetersPerDeviceIndependentPixel = 25.4d / 96d;
        var dropPosition = e.GetPosition(DesignerCanvas);
        var element = viewModel.AddElementAt(
            kind,
            dropPosition.X * millimetersPerDeviceIndependentPixel,
            dropPosition.Y * millimetersPerDeviceIndependentPixel,
            beginEditing: kind == LabelElementKind.Text);

        if (kind == LabelElementKind.Text)
        {
            BeginInlineEdit(element);
        }

        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void AddTextButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            BeginInlineEdit(viewModel.AddElementAt(LabelElementKind.Text, 5, 5, beginEditing: true));
        }
    }

    private void DesignerItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Thumb { DataContext: LabelElementViewModel element } &&
            DataContext is MainViewModel viewModel)
        {
            viewModel.SelectedElement = element;
            if (e.ClickCount == 2 && element.Kind == LabelElementKind.Text)
            {
                BeginInlineEdit(element);
                e.Handled = true;
            }
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
        if (sender is not Thumb { DataContext: LabelElementViewModel element } resizeThumb ||
            DataContext is not MainViewModel viewModel)
        {
            return;
        }

        const double millimetersPerDeviceIndependentPixel = 25.4d / 96d;
        var direction = resizeThumb.Tag as string ?? "SE";
        var horizontalChange = e.HorizontalChange * millimetersPerDeviceIndependentPixel;
        var verticalChange = e.VerticalChange * millimetersPerDeviceIndependentPixel;

        if (element.Kind == LabelElementKind.Line && direction is "LineStart" or "LineEnd")
        {
            ResizeLineEndpoint(
                element,
                direction,
                horizontalChange,
                verticalChange,
                viewModel.LabelWidth,
                viewModel.LabelHeight);
            viewModel.SelectedElement = element;
            e.Handled = true;
            return;
        }

        var minimumWidth = element.Kind == LabelElementKind.Line ? 0.25 : 1;
        var minimumHeight = element.Kind == LabelElementKind.Line ? 0.1 : 1;

        var left = element.X;
        var top = element.Y;
        var right = element.X + element.Width;
        var bottom = element.Y + element.Height;

        if (direction.Contains('W'))
        {
            left = Math.Clamp(left + horizontalChange, 0, right - minimumWidth);
        }
        else if (direction.Contains('E'))
        {
            right = Math.Clamp(right + horizontalChange, left + minimumWidth, viewModel.LabelWidth);
        }

        if (direction.Contains('N'))
        {
            top = Math.Clamp(top + verticalChange, 0, bottom - minimumHeight);
        }
        else if (direction.Contains('S'))
        {
            bottom = Math.Clamp(bottom + verticalChange, top + minimumHeight, viewModel.LabelHeight);
        }

        element.X = left;
        element.Y = top;
        element.Width = right - left;
        element.Height = bottom - top;
        viewModel.SelectedElement = element;
        e.Handled = true;
    }

    private static void ResizeLineEndpoint(
        LabelElementViewModel element,
        string endpoint,
        double horizontalChange,
        double verticalChange,
        double labelWidth,
        double labelHeight)
    {
        const double minimumLineSpan = 0.1;
        var left = element.X;
        var right = element.X + element.Width;
        var leftY = element.IsLineDirectionReversed
            ? element.Y + element.Height
            : element.Y;
        var rightY = element.IsLineDirectionReversed
            ? element.Y
            : element.Y + element.Height;

        if (endpoint == "LineStart")
        {
            left = Math.Clamp(left + horizontalChange, 0, right - minimumLineSpan);
            leftY = Math.Clamp(leftY + verticalChange, 0, labelHeight);
        }
        else
        {
            right = Math.Clamp(right + horizontalChange, left + minimumLineSpan, labelWidth);
            rightY = Math.Clamp(rightY + verticalChange, 0, labelHeight);
        }

        element.X = left;
        element.Y = Math.Min(leftY, rightY);
        element.Width = right - left;
        element.Height = Math.Max(minimumLineSpan, Math.Abs(rightY - leftY));
        element.IsLineDirectionReversed = leftY > rightY;
    }

    private void InlineTextEditor_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox { DataContext: LabelElementViewModel element } &&
            ReferenceEquals(element, inlineEditingElement))
        {
            CompleteInlineEdit(cancel: false);
        }
    }

    private void InlineTextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            CompleteInlineEdit(cancel: false);
            DesignerCanvas.Focus();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CompleteInlineEdit(cancel: true);
            DesignerCanvas.Focus();
            e.Handled = true;
        }
    }

    private void BeginInlineEdit(LabelElementViewModel element)
    {
        if (DataContext is not MainViewModel viewModel || element.Kind != LabelElementKind.Text)
        {
            return;
        }

        CompleteInlineEdit(cancel: false);
        foreach (var candidate in viewModel.Elements)
        {
            candidate.IsEditing = false;
        }

        viewModel.SelectedElement = element;
        inlineEditingElement = element;
        inlineEditOriginalContent = element.Content;
        element.IsEditing = true;

        Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            if (DesignerCanvas.ItemContainerGenerator.ContainerFromItem(element) is not ListBoxItem container)
            {
                return;
            }

            var editor = FindVisualChild<TextBox>(container, "InlineTextEditor");
            editor?.Focus();
            editor?.SelectAll();
        });
    }

    private void CompleteInlineEdit(bool cancel)
    {
        var element = inlineEditingElement;
        var originalContent = inlineEditOriginalContent;
        inlineEditingElement = null;
        inlineEditOriginalContent = null;

        if (element is null)
        {
            return;
        }

        if (cancel && originalContent is not null)
        {
            element.Content = originalContent;
        }

        element.IsEditing = false;
    }

    private static T? FindVisualChild<T>(DependencyObject parent, string name)
        where T : FrameworkElement
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match && match.Name == name)
            {
                return match;
            }

            var descendant = FindVisualChild<T>(child, name);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}
