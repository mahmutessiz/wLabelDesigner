using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
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
    private bool isSynchronizingCanvasSelection;
    private bool isPanning;
    private Point panStart;
    private double panHorizontalOffset;
    private double panVerticalOffset;
    private double panTranslateX;
    private double panTranslateY;
    private DesignerBounds? dragInitialBounds;
    private DesignerBounds[] dragOtherBounds = [];
    private double dragRawHorizontalChange;
    private double dragRawVerticalChange;
    private double dragAppliedHorizontalChange;
    private double dragAppliedVerticalChange;
    private LabelElementViewModel? rotatingElement;
    private double rotationStartAngle;
    private double rotationPointerStartAngle;

    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainViewModel(
            new JsonLabelDocumentStore(),
            new FileDialogService(),
            new WpfLabelPrintService(),
            new WpfElementClipboard());
        viewModel.PropertyChanged += MainViewModel_PropertyChanged;
        DataContext = viewModel;
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1)
        {
            ShowHelp();
            e.Handled = true;
            return;
        }

        if (Keyboard.FocusedElement is TextBoxBase or ComboBox || DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var controlPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        var shiftPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

        if (controlPressed && e.Key is Key.OemPlus or Key.Add or Key.OemMinus or Key.Subtract or Key.D0 or Key.NumPad0)
        {
            if (e.Key is Key.OemPlus or Key.Add)
            {
                viewModel.ZoomInCommand.Execute(null);
            }
            else if (e.Key is Key.OemMinus or Key.Subtract)
            {
                viewModel.ZoomOutCommand.Execute(null);
            }
            else
            {
                viewModel.ResetZoomCommand.Execute(null);
            }

            e.Handled = true;
            return;
        }

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
        else if (e.Key == Key.Escape)
        {
            command = viewModel.DeselectAllCommand;
        }

        if (command?.CanExecute(parameter) == true)
        {
            command.Execute(parameter);
            e.Handled = true;
        }
    }

    private void Workspace_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source ||
            FindVisualAncestor<ListBoxItem>(source) is not null ||
            DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (viewModel.DeselectAllCommand.CanExecute(null))
        {
            viewModel.DeselectAllCommand.Execute(null);
        }

        DesignerCanvas.Focus();
    }

    private void Help_Click(object sender, RoutedEventArgs e) => ShowHelp();

    private void ShowHelp()
    {
        var helpWindow = new HelpWindow
        {
            Owner = this
        };
        helpWindow.ShowDialog();
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
        if (DataContext is MainViewModel viewModel)
        {
            isSynchronizingCanvasSelection = true;
            try
            {
                viewModel.SetSelectionFromView(
                    DesignerCanvas.SelectedItems.Cast<LabelElementViewModel>(),
                    DesignerCanvas.SelectedItem as LabelElementViewModel);
            }
            finally
            {
                isSynchronizingCanvasSelection = false;
            }
        }

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

    private void MainViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (isSynchronizingCanvasSelection ||
            e.PropertyName != nameof(MainViewModel.SelectedElement) ||
            sender is not MainViewModel viewModel)
        {
            return;
        }

        Dispatcher.BeginInvoke(DispatcherPriority.DataBind, () =>
        {
            isSynchronizingCanvasSelection = true;
            try
            {
                DesignerCanvas.UnselectAll();
                if (viewModel.SelectedElement is not null &&
                    DesignerCanvas.ItemContainerGenerator.ContainerFromItem(viewModel.SelectedElement) is ListBoxItem container)
                {
                    container.IsSelected = true;
                }
            }
            finally
            {
                isSynchronizingCanvasSelection = false;
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
            var container = FindVisualAncestor<ListBoxItem>((DependencyObject)sender);
            if (container is not null && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                container.IsSelected = !container.IsSelected;
                e.Handled = true;
                return;
            }

            if (container is not null && !container.IsSelected)
            {
                DesignerCanvas.UnselectAll();
                container.IsSelected = true;
            }

            if (e.ClickCount == 2 && !element.IsLocked && element.Kind == LabelElementKind.Text)
            {
                BeginInlineEdit(element);
                e.Handled = true;
            }
        }
    }

    private void DesignerItem_DragStarted(object sender, DragStartedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel || viewModel.SelectedElements.Count == 0)
        {
            return;
        }

        var selection = viewModel.SelectedElements.Where(candidate => !candidate.IsLocked).ToArray();
        if (selection.Length == 0)
        {
            return;
        }
        dragInitialBounds = new DesignerBounds(
            selection.Min(candidate => candidate.X),
            selection.Min(candidate => candidate.Y),
            selection.Max(candidate => candidate.X + candidate.Width) - selection.Min(candidate => candidate.X),
            selection.Max(candidate => candidate.Y + candidate.Height) - selection.Min(candidate => candidate.Y));
        var selectedIds = selection.Select(candidate => candidate.Id).ToHashSet();
        dragOtherBounds = viewModel.Elements
            .Where(candidate => candidate.IsVisible && !selectedIds.Contains(candidate.Id))
            .Select(candidate => new DesignerBounds(candidate.X, candidate.Y, candidate.Width, candidate.Height))
            .ToArray();
        dragRawHorizontalChange = 0;
        dragRawVerticalChange = 0;
        dragAppliedHorizontalChange = 0;
        dragAppliedVerticalChange = 0;
    }

    private void DesignerItem_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is not Thumb { DataContext: LabelElementViewModel { IsLocked: false } } ||
            DataContext is not MainViewModel viewModel)
        {
            return;
        }

        const double millimetersPerDeviceIndependentPixel = 25.4d / 96d;
        dragRawHorizontalChange += e.HorizontalChange * millimetersPerDeviceIndependentPixel;
        dragRawVerticalChange += e.VerticalChange * millimetersPerDeviceIndependentPixel;
        if (dragInitialBounds is DesignerBounds selectionBounds)
        {
            var guides = DesignerGuideEngine.FindGuides(
                selectionBounds,
                dragRawHorizontalChange,
                dragRawVerticalChange,
                viewModel.LabelWidth,
                viewModel.LabelHeight,
                5 * millimetersPerDeviceIndependentPixel / viewModel.ZoomScale,
                dragOtherBounds);
            ShowAlignmentGuides(guides);
        }
        else
        {
            HideAlignmentGuides();
        }

        viewModel.MoveSelectedElements(
            dragRawHorizontalChange - dragAppliedHorizontalChange,
            dragRawVerticalChange - dragAppliedVerticalChange);
        if (dragInitialBounds is DesignerBounds initialBounds)
        {
            var movedElements = viewModel.SelectedElements.Where(candidate => !candidate.IsLocked).ToArray();
            dragAppliedHorizontalChange = movedElements.Min(candidate => candidate.X) - initialBounds.X;
            dragAppliedVerticalChange = movedElements.Min(candidate => candidate.Y) - initialBounds.Y;
        }
        e.Handled = true;
    }

    private void DesignerItem_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        dragInitialBounds = null;
        dragOtherBounds = [];
        HideAlignmentGuides();
    }

    private void ShowAlignmentGuides(AlignmentGuideResult guides)
    {
        const double pixelsPerMillimeter = 96d / 25.4d;
        VerticalAlignmentGuide.Visibility = guides.VerticalGuide is null ? Visibility.Collapsed : Visibility.Visible;
        HorizontalAlignmentGuide.Visibility = guides.HorizontalGuide is null ? Visibility.Collapsed : Visibility.Visible;
        if (guides.VerticalGuide is double x)
        {
            VerticalAlignmentGuide.X1 = VerticalAlignmentGuide.X2 = x * pixelsPerMillimeter;
        }

        if (guides.HorizontalGuide is double y)
        {
            HorizontalAlignmentGuide.Y1 = HorizontalAlignmentGuide.Y2 = y * pixelsPerMillimeter;
        }
    }

    private void HideAlignmentGuides()
    {
        VerticalAlignmentGuide.Visibility = Visibility.Collapsed;
        HorizontalAlignmentGuide.Visibility = Visibility.Collapsed;
    }

    private void Workspace_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var pointer = e.GetPosition(WorkspaceScrollViewer);
        var oldScale = viewModel.ZoomScale;
        viewModel.SetZoom(viewModel.ZoomPercent * (e.Delta > 0 ? 1.1 : 1 / 1.1));
        var scaleRatio = viewModel.ZoomScale / oldScale;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            WorkspaceScrollViewer.ScrollToHorizontalOffset(
                ((WorkspaceScrollViewer.HorizontalOffset + pointer.X) * scaleRatio) - pointer.X);
            WorkspaceScrollViewer.ScrollToVerticalOffset(
                ((WorkspaceScrollViewer.VerticalOffset + pointer.Y) * scaleRatio) - pointer.Y);
        });
        e.Handled = true;
    }

    private void Workspace_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        var spacePressed = Keyboard.IsKeyDown(Key.Space);
        if (e.ChangedButton != MouseButton.Middle && !(e.ChangedButton == MouseButton.Left && spacePressed))
        {
            return;
        }

        isPanning = true;
        panStart = e.GetPosition(WorkspaceScrollViewer);
        panHorizontalOffset = WorkspaceScrollViewer.HorizontalOffset;
        panVerticalOffset = WorkspaceScrollViewer.VerticalOffset;
        panTranslateX = CanvasPanTransform.X;
        panTranslateY = CanvasPanTransform.Y;
        WorkspaceScrollViewer.Cursor = Cursors.Hand;
        WorkspaceScrollViewer.CaptureMouse();
        e.Handled = true;
    }

    private void Workspace_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!isPanning)
        {
            return;
        }

        var current = e.GetPosition(WorkspaceScrollViewer);
        var horizontalChange = current.X - panStart.X;
        var verticalChange = current.Y - panStart.Y;
        WorkspaceScrollViewer.ScrollToHorizontalOffset(panHorizontalOffset - horizontalChange);
        WorkspaceScrollViewer.ScrollToVerticalOffset(panVerticalOffset - verticalChange);

        CanvasPanTransform.X = panTranslateX + horizontalChange +
            WorkspaceScrollViewer.HorizontalOffset - panHorizontalOffset;
        CanvasPanTransform.Y = panTranslateY + verticalChange +
            WorkspaceScrollViewer.VerticalOffset - panVerticalOffset;
        e.Handled = true;
    }

    private void Workspace_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!isPanning)
        {
            return;
        }

        isPanning = false;
        WorkspaceScrollViewer.ReleaseMouseCapture();
        WorkspaceScrollViewer.Cursor = Cursors.Arrow;
        e.Handled = true;
    }

    private void Workspace_LostMouseCapture(object sender, MouseEventArgs e)
    {
        isPanning = false;
        WorkspaceScrollViewer.Cursor = Cursors.Arrow;
    }

    private void DesignerItem_ResizeDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is not Thumb { DataContext: LabelElementViewModel { IsLocked: false } element } resizeThumb ||
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

    private void DesignerItem_RotationStarted(object sender, DragStartedEventArgs e)
    {
        if (sender is not Thumb { DataContext: LabelElementViewModel { IsLocked: false } element })
        {
            return;
        }

        rotatingElement = element;
        rotationStartAngle = element.RotationDegrees;
        rotationPointerStartAngle = GetPointerAngle(element);
        e.Handled = true;
    }

    private void DesignerItem_RotationDelta(object sender, DragDeltaEventArgs e)
    {
        if (rotatingElement is not LabelElementViewModel element)
        {
            return;
        }

        var pointerDelta = NormalizeAngle(GetPointerAngle(element) - rotationPointerStartAngle);
        var angle = NormalizeAngle(rotationStartAngle + pointerDelta);
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            angle = Math.Round(angle / 15d, MidpointRounding.AwayFromZero) * 15;
        }

        element.RotationDegrees = angle;
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.SelectedElement = element;
            viewModel.StatusMessage = $"Rotation {element.RotationDegrees:0.##}°";
        }

        e.Handled = true;
    }

    private void DesignerItem_RotationCompleted(object sender, DragCompletedEventArgs e)
    {
        rotatingElement = null;
        e.Handled = true;
    }

    private double GetPointerAngle(LabelElementViewModel element)
    {
        const double pixelsPerMillimeter = 96d / 25.4d;
        var pointer = Mouse.GetPosition(DesignerCanvas);
        var centerX = (element.X + (element.Width / 2)) * pixelsPerMillimeter;
        var centerY = (element.Y + (element.Height / 2)) * pixelsPerMillimeter;
        return (Math.Atan2(pointer.Y - centerY, pointer.X - centerX) * 180d / Math.PI) + 90d;
    }

    private static double NormalizeAngle(double angle)
    {
        var normalized = angle % 360;
        if (normalized > 180)
        {
            normalized -= 360;
        }
        else if (normalized < -180)
        {
            normalized += 360;
        }

        return normalized;
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
        if (DataContext is not MainViewModel viewModel || element.IsLocked || element.Kind != LabelElementKind.Text)
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

    private static T? FindVisualAncestor<T>(DependencyObject child)
        where T : DependencyObject
    {
        var current = child;
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}
