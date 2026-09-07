using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using wLabelDesigner.Controls;
using wLabelDesigner.Models;
using wLabelDesigner.Services;
using wLabelDesigner.ViewModels;
using Xunit;
using LineGeometry = wLabelDesigner.Services.LineGeometry;

namespace wLabelDesigner.Tests;

public sealed class LineInteractionTests
{
    [Fact]
    public async Task Designer_NewLineAndEditedStrokeAreVisibleWithoutClipping()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var app = new App();
                app.InitializeComponent();
                var window = new MainWindow();
                var viewModel = (MainViewModel)window.DataContext;
                var line = viewModel.AddElementAt(LabelElementKind.Line, 20, 20);
                var root = (FrameworkElement)window.Content;
                window.Content = null;
                root.DataContext = viewModel;
                using var source = new System.Windows.Interop.HwndSource(new System.Windows.Interop.HwndSourceParameters("Line rendering test")
                {
                    Width = 1400,
                    Height = 850,
                    WindowStyle = unchecked((int)0x80000000)
                });
                source.RootVisual = root;
                root.Measure(new Size(1400, 850));
                root.Arrange(new Rect(0, 0, 1400, 850));
                root.UpdateLayout();
                var canvas = (ListBox)window.FindName("DesignerCanvas");
                var container = (ListBoxItem)canvas.ItemContainerGenerator.ContainerFromItem(line);
                var thumb = FindVisual<LineMoveThumb>(container)!;
                Assert.NotNull(thumb);

                // Inspect the actual designer, including its item-container layout.
                Assert.True(CountPixels(canvas, thumb, red: false) > 0, "A newly added line must be visible.");
                ((ComboBox)window.FindName("LineStrokeColor")).SelectedValue = "#FFD92D20";
                ((ComboBox)window.FindName("LineStrokeWidth")).Text = "12";
                root.UpdateLayout();
                Assert.Equal(12, line.StrokeThickness);
                Assert.Equal(12, thumb.StrokeThickness);
                Assert.True(CountPixels(canvas, thumb, red: true) >= 11, "A 12px stroke must extend on both sides of the zero-height line.");
                ((ComboBox)window.FindName("LineStrokeWidth")).Text = "20";
                root.UpdateLayout();
                Assert.True(CountPixels(canvas, thumb, red: true) >= 19, "The maximum stroke width must not be clipped.");
                ((ComboBox)window.FindName("LineStrokeStyle")).SelectedValue = StrokeStyleOption.Dashed;
                root.UpdateLayout();
                Assert.Equal(StrokeStyleOption.Dashed, thumb.StrokeStyle);
                viewModel.FitLineCommand.Execute("Height");
                root.UpdateLayout();
                Assert.Equal(0, line.Width);
                Assert.True(CountPixels(canvas, thumb, red: true, vertical: true) > 0, "A vertical line must remain visible.");
                var rotationBox = (TextBox)window.FindName("RotationTextBox");
                var rotationSlider = (Slider)window.FindName("RotationSlider");
                Assert.Equal("90", rotationBox.Text);
                viewModel.SetLineEndpoints(line, new(10, 20), new(30, 0));
                root.UpdateLayout();
                Assert.Equal("-45", rotationBox.Text);
                Assert.Equal(-45, rotationSlider.Value);
                line.RotationDegrees = 15;
                root.UpdateLayout();
                Assert.Equal("-30", rotationBox.Text);
                rotationBox.Text = "30";
                root.UpdateLayout();
                Assert.Equal(30, line.DisplayRotationDegrees);
                Assert.Equal(30, rotationSlider.Value);
                var endpoints = LineGeometry.GetEndpoints(line.ToData());
                Assert.Equal(30, Math.Atan2(endpoints.End.Y - endpoints.Start.Y, endpoints.End.X - endpoints.Start.X) * 180 / Math.PI, 2);
                rotationSlider.Value = -60;
                root.UpdateLayout();
                Assert.Equal("-60", rotationBox.Text);
                typeof(MainWindow).GetMethod("CloseWithoutPrompt", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(window, null);
                completion.SetResult();
            }
            catch (Exception exception) { completion.SetException(exception); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await completion.Task;
    }

    private static T? FindVisual<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) return match;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            if (FindVisual<T>(VisualTreeHelper.GetChild(root, index)) is { } child) return child;
        }
        return null;
    }

    private static int CountPixels(FrameworkElement canvas, LineMoveThumb thumb, bool red, bool vertical = false)
    {
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(canvas.ActualWidth), (int)Math.Ceiling(canvas.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var bounds = new Rect(0, 0, canvas.ActualWidth, canvas.ActualHeight);
            context.DrawRectangle(Brushes.White, null, bounds);
            context.DrawRectangle(new VisualBrush(canvas) { ViewboxUnits = BrushMappingMode.Absolute, Viewbox = bounds, Stretch = Stretch.Fill }, null, bounds);
        }
        bitmap.Render(visual);
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        var center = thumb.TranslatePoint(new Point(
            thumb.DrawingInset + (thumb.ActualWidth - 2 * thumb.DrawingInset) / 3,
            thumb.DrawingInset + (thumb.ActualHeight - 2 * thumb.DrawingInset) / 3), canvas);
        var count = 0;
        for (var offset = -15; offset <= 15; offset++)
        {
            var x = (int)center.X + (vertical ? offset : 0);
            var y = (int)center.Y + (vertical ? 0 : offset);
            var index = (y * bitmap.PixelWidth + x) * 4;
            if (red ? pixels[index + 2] > 150 && pixels[index + 1] < 100 && pixels[index] < 100
                : pixels[index] < 200 && pixels[index + 1] < 200 && pixels[index + 2] < 200 && pixels[index + 3] > 200)
                count++;
        }
        return count;
    }

    [Theory]
    [InlineData(80, 0)]
    [InlineData(0, 30)]
    public async Task StraightLine_RoundTripsAndRenders(double width, double height)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".wld");
        var document = new LabelDocument
        {
            Elements = [new LabelElementData { Kind = LabelElementKind.Line, X = 5, Y = 5, Width = width, Height = height }]
        };
        try
        {
            var store = new JsonLabelDocumentStore();
            await store.SaveAsync(path, document);
            var reopened = await store.LoadAsync(path);
            Assert.Equal(width, reopened.Elements[0].Width);
            Assert.Equal(height, reopened.Elements[0].Height);
            var drawing = LabelDrawingRenderer.CreateDrawing(reopened);
            Assert.False(drawing.Bounds.IsEmpty);
            Assert.NotEmpty(LabelPngRenderer.Render(reopened));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void RotatedLine_EndpointsMatchVisibleRotation()
    {
        var (start, end) = LineGeometry.GetEndpoints(new LabelElementData
        {
            Kind = LabelElementKind.Line,
            X = 20,
            Y = 20,
            Width = 20,
            Height = 0,
            RotationDegrees = 90
        });
        Assert.Equal(30, start.X, 8);
        Assert.Equal(10, start.Y, 8);
        Assert.Equal(30, end.X, 8);
        Assert.Equal(30, end.Y, 8);
    }

    [Fact]
    public void Endpoint_SnapsToAxisAndClampsToLabel()
    {
        Assert.Equal(new LinePoint(100, 10), LineGeometry.ConstrainEndpoint(new(120, 12), new(10, 10), 100, 50, true));
        Assert.Equal(new LinePoint(10, 50), LineGeometry.ConstrainEndpoint(new(12, 90), new(10, 10), 100, 50, true));
        Assert.Equal(new LinePoint(0, 12), LineGeometry.ConstrainEndpoint(new(-5, 12), new(10, 10), 100, 50, false));
    }

    [Theory]
    [InlineData(100, 0, 50, 5)]
    [InlineData(0, 100, 5, 50)]
    [InlineData(100, 100, 50, 55)]
    public async Task LineGrabTarget_IncludesSpaceAroundThinStroke(double width, double height, double hitX, double hitY)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var canvas = new Canvas { Width = 200, Height = 200 };
                var thumb = new LineMoveThumb { Width = width, Height = height, Template = new ControlTemplate(typeof(Thumb)) };
                canvas.Children.Add(thumb);
                canvas.Measure(new Size(200, 200));
                canvas.Arrange(new Rect(0, 0, 200, 200));
                canvas.UpdateLayout();
                Assert.Same(thumb, VisualTreeHelper.HitTest(canvas, new Point(hitX, hitY))?.VisualHit);
                completion.SetResult();
            }
            catch (Exception exception) { completion.SetException(exception); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await completion.Task;
    }
}
