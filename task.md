# wLabelDesigner Roadmap

## Recommended implementation order

- [x] Add undo and redo for edits, movement, resizing, deletion, and styling.
- [ ] Add copy, paste, duplicate, and keyboard nudging.
- [ ] Add multi-selection with alignment and distribution tools.
- [ ] Add zoom, pan, rulers, grid, snapping, and alignment guides.
- [ ] Add rotation handles for text, shapes, barcodes, QR codes, and images.
- [ ] Add layer ordering, locking, and visibility controls.
- [ ] Add an image/logo element.
- [ ] Add CSV variables and batch printing.

## Designer interaction

- [x] Undo and redo every document-changing action.
- [ ] Copy, paste, duplicate, and cut selected elements.
- [ ] Move selected elements using the arrow keys.
- [ ] Support multi-selection using Shift-click and selection rectangles.
- [ ] Align selected elements to the left, center, right, top, middle, or bottom.
- [ ] Distribute selected elements horizontally or vertically.
- [ ] Add zoom controls and mouse-wheel zooming.
- [ ] Add canvas panning.
- [ ] Add horizontal and vertical rulers.
- [ ] Add configurable grid visibility and grid size.
- [ ] Snap elements to the grid, label edges, and other elements.
- [ ] Display temporary alignment guides while moving elements.
- [ ] Add rotation handles and precise rotation values.
- [ ] Add bring forward, send backward, bring to front, and send to back.
- [ ] Allow elements to be locked and hidden.

## Styling

- [ ] Add fill-color selection.
- [ ] Add border and stroke-color selection.
- [ ] Add opacity controls.
- [ ] Add configurable corner radius.
- [ ] Add solid, dashed, and dotted stroke styles.
- [ ] Add vertical text alignment.
- [ ] Add text line-spacing controls.
- [ ] Add letter-spacing controls.
- [ ] Add automatic text fitting and overflow indicators.

## Elements

- [ ] Add an image/logo element.
- [ ] Support image crop, contain, cover, and stretch modes.
- [ ] Allow image aspect ratio to be locked.
- [ ] Add more basic shapes as required.

## Barcode and QR options

- [ ] Support Code 128, EAN-13, UPC, and other required barcode formats.
- [ ] Validate barcode content for the selected format.
- [ ] Allow barcode human-readable labels to be shown or hidden.
- [ ] Add barcode quiet-zone controls.
- [ ] Add QR error-correction settings.
- [ ] Add QR margin controls.
- [ ] Add barcode and QR foreground/background colors.
- [ ] Allow a logo to be embedded in QR codes.

## Data and batch printing

- [ ] Import CSV files as data sources.
- [ ] Add variable fields such as `{ProductName}`, `{Price}`, and `{Barcode}`.
- [ ] Preview a label using any selected CSV row.
- [ ] Print one label per CSV row.
- [ ] Support configurable copies per row.
- [ ] Report invalid or missing variable values before printing.

## Printing and export

- [ ] Add printer margin and X/Y offset calibration.
- [ ] Add thermal-printer darkness and speed settings where supported.
- [ ] Add a calibration and test-print workflow.
- [ ] Export labels to PNG.
- [ ] Export labels to PDF.

## Reliability and document workflow

- [ ] Warn before closing or replacing a document with unsaved changes.
- [ ] Add autosave and crash recovery.
- [ ] Add a recent-files list.
- [ ] Validate imported template files and report actionable errors.
- [ ] Add automated tests for editing, serialization, rendering, and printing behavior.
