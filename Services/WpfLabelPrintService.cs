using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using wLabelDesigner.Models;
using wLabelDesigner.ViewModels;

namespace wLabelDesigner.Services;

public sealed class WpfLabelPrintService : ILabelPrintService
{
    public bool Print(LabelDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var printers = GetPrinterNames(out var defaultPrinter);
        var viewModel = new PrintPreviewViewModel(document, printers, defaultPrinter);
        var previewWindow = new PrintPreviewWindow(viewModel)
        {
            Owner = Application.Current.MainWindow
        };

        if (previewWindow.ShowDialog() != true || viewModel.SelectedPrinter is null)
        {
            return false;
        }

        document.PrintSettings.Copies = viewModel.Copies;
        document.PrintSettings.PrinterName = viewModel.SelectedPrinter;
        document.PrintSettings.MarginLeftMillimeters = viewModel.MarginLeftMillimeters;
        document.PrintSettings.MarginTopMillimeters = viewModel.MarginTopMillimeters;
        document.PrintSettings.MarginRightMillimeters = viewModel.MarginRightMillimeters;
        document.PrintSettings.MarginBottomMillimeters = viewModel.MarginBottomMillimeters;
        document.PrintSettings.OffsetXMillimeters = viewModel.OffsetXMillimeters;
        document.PrintSettings.OffsetYMillimeters = viewModel.OffsetYMillimeters;
        PrintToSelectedPrinter(document);
        return true;
    }

    private static IReadOnlyList<string> GetPrinterNames(out string? defaultPrinter)
    {
        try
        {
            using var server = new LocalPrintServer();
            using var defaultQueue = LocalPrintServer.GetDefaultPrintQueue();
            using var queues = server.GetPrintQueues();

            defaultPrinter = defaultQueue?.FullName;
            return queues
                .Select(queue => queue.FullName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
        }
        catch (PrintSystemException)
        {
            defaultPrinter = null;
            return [];
        }
    }

    private static void PrintToSelectedPrinter(LabelDocument document)
    {
        using var server = new LocalPrintServer();
        using var queues = server.GetPrintQueues();
        var queue = queues.FirstOrDefault(candidate =>
            string.Equals(candidate.FullName, document.PrintSettings.PrinterName, StringComparison.OrdinalIgnoreCase));

        if (queue is null)
        {
            throw new PrintQueueException("The selected printer is no longer available.");
        }

        var dialog = new PrintDialog
        {
            PrintQueue = queue,
            PrintTicket = queue.DefaultPrintTicket
        };
        dialog.PrintTicket.CopyCount = Math.Clamp(document.PrintSettings.Copies, 1, 999);
        dialog.PrintTicket.PageMediaSize = new PageMediaSize(
            document.WidthMillimeters * LabelDrawingRenderer.DeviceIndependentPixelsPerMillimeter,
            document.HeightMillimeters * LabelDrawingRenderer.DeviceIndependentPixelsPerMillimeter);

        // Submit one bounded, printer-resolution bitmap instead of a complex WPF
        // drawing. Many thermal-printer drivers otherwise rasterize every vector,
        // font, barcode, and image inside the spooler process, causing CPU spikes.
        var printBitmap = LabelPngRenderer.RenderPrintBitmap(document, document.PrintSettings);
        var pageBounds = new Rect(
            0,
            0,
            document.WidthMillimeters * LabelDrawingRenderer.DeviceIndependentPixelsPerMillimeter,
            document.HeightMillimeters * LabelDrawingRenderer.DeviceIndependentPixelsPerMillimeter);
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawImage(printBitmap, pageBounds);
        }

        dialog.PrintVisual(visual, document.Name);
    }
}
