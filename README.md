# wLabelDesigner

wLabelDesigner is a lightweight Windows desktop application for designing and printing thermal labels. It targets common 203 and 300 DPI label printers and stores editable templates as JSON-based `.fckbartndr` files.

The application is built with .NET 10, WPF, and MVVM.

## Screenshots

### Welcome screen

![wLabelDesigner welcome screen](Resources/pic1.png)

### Label editor

![wLabelDesigner label editor](Resources/pic2.png)

## Current features

- Welcome screen with recent templates and cheese-pallet shipping, general shipping, product, shelf, and QR contact starter layouts.
- Canvas-based label editor using physical millimetre dimensions.
- Text, Code 128 barcode, QR code, box, rounded-box, and line elements.
- Drag-to-place element palette.
- Direct element movement and eight-handle resizing.
- Independent line endpoint editing.
- Multiline, in-place text editing.
- Font family, size, bold, italic, underline, and alignment controls.
- Configurable shape stroke thickness.
- Shift-click multi-selection.
- Group movement, alignment, and distribution.
- Undo and redo with coalesced typing and drag operations.
- Cut, copy, paste, duplicate, delete, and keyboard nudging.
- Print preview with printer selection and copy count.
- Saved printer and copy defaults in each template.
- Context-sensitive barcode and QR data panel.
- Built-in F1 user guide.

## Requirements

- Windows 10 or Windows 11, x64.
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) to run the application.
- .NET 10 SDK to build from source.
- A Windows printer configured for physical printing.

## Build and run

From PowerShell in the repository root:

```powershell
dotnet restore .\wLabelDesigner.csproj
dotnet build .\wLabelDesigner.csproj --configuration Debug
dotnet run --project .\wLabelDesigner.csproj
```

The Debug executable is written to:

```text
bin\Debug\net10.0-windows\wLabelDesigner.exe
```

Close any running copy of the application before rebuilding the normal output path because Windows locks the active executable.

## Basic workflow

1. Choose a recent template or starter layout, create a blank label, or browse for a saved template from the welcome screen.
2. Set the label name, width, height, and printer DPI in the top bar if needed.
3. Drag an element from the left tool rail onto the white label.
4. Drag an element to move it and use its handles to resize it.
5. Double-click text to edit it directly on the label.
6. Select a barcode or QR code to edit its encoded content in the right panel.
7. Use Shift-click to select multiple elements, then align or distribute them from the contextual top bar.
8. Save the editable template as a `.fckbartndr` file.
9. Press Print to review the label, select a printer, choose the copy count, and print.

Click empty workspace or press Escape to clear the current selection. Selected elements are temporarily displayed above overlapping elements while editing without changing the saved or printed layer order.

Use **Welcome** in the main toolbar or **File → Welcome screen** to return to the starter layouts. If the current label has unsaved changes, the application asks whether to save it before replacing the document.

## Keyboard shortcuts

| Shortcut | Action |
| --- | --- |
| `Ctrl+N` | New label |
| `Ctrl+O` | Open template |
| `Ctrl+S` | Save template |
| `Ctrl+P` | Open print preview |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` or `Ctrl+Shift+Z` | Redo |
| `Ctrl+C` | Copy selected element |
| `Ctrl+X` | Cut selected element |
| `Ctrl+V` | Paste element |
| `Ctrl+D` | Duplicate selected element |
| `Delete` | Delete selected elements |
| `Escape` | Clear selection or cancel inline text editing |
| `Tab` / `Shift+Tab` | Select next or previous element |
| Arrow keys | Move selection by 0.5 mm |
| `Shift` + arrow keys | Move selection by 5 mm |
| `Shift` + click | Toggle an element in the selection |
| Double-click text | Begin inline text editing |
| `Enter` | Insert a new line while editing text |
| `Ctrl+Enter` | Commit inline text editing |
| `F1` | Open the user guide |

Keyboard editing commands do not override normal input behavior while a text or combo-box field has focus.

## Template format

Templates use the `.fckbartndr` extension and contain human-readable JSON. A template stores:

- Format version and document name.
- Label width, height, and printer DPI.
- Element types, IDs, geometry, content, and styling.
- Line direction and shape stroke settings.
- Preferred printer and default copy count.

The current template format version is 2. Older version 1 templates remain loadable.

## NuGet dependencies

- `CommunityToolkit.Mvvm` for MVVM observable properties and commands.
- `BarcodeLib` for Code 128 rendering.
- `QRCoder` for QR-code rendering.
- `CsvHelper` for planned variable-data and batch-printing support.

## Project structure

```text
Converters/   WPF value converters and preview conversion
Docs/         Product specification
Models/       Serializable label document and element models
Services/     Persistence, clipboard, rendering, printing, and history
ViewModels/   Designer and print-preview presentation state
*.xaml        Main designer, help, and print-preview windows
```

Engineering conventions are documented in [AGENTS.md](AGENTS.md). Planned work is tracked in [task.md](task.md).

## Current limitations

- Barcode output currently uses Code 128.
- CSV variable fields and batch printing are not implemented yet.
- Canvas zoom, rulers, grid snapping, rotation, and persistent layer controls remain planned.
- Physical output may require printer-specific margin or offset calibration.

## License

This project is licensed under the [GNU General Public License version 3](LICENSE).
