# wLabelDesigner Roadmap

## Recommended implementation order

- [x] Add undo and redo for edits, movement, resizing, deletion, and styling.
- [x] Add copy, paste, duplicate, and keyboard nudging.
- [x] Add multi-selection with alignment and distribution tools.
- [x] Add zoom, pan, rulers, grid, and alignment guides.
- [x] Add rotation handles for text, shapes, barcodes, QR codes, and images.
- [x] Add layer ordering, locking, and visibility controls.
- [x] Add an image/logo element.

## Designer interaction

- [x] Undo and redo every document-changing action.
- [x] Copy, paste, duplicate, and cut selected elements.
- [x] Move selected elements using the arrow keys.
- [x] Support multi-selection using Shift-click.
- [x] Align selected elements to the left, center, right, top, middle, or bottom.
- [x] Distribute selected elements horizontally or vertically.
- [x] Add zoom controls and mouse-wheel zooming.
- [x] Add canvas panning.
- [x] Add horizontal and vertical rulers.
- [x] Add configurable grid visibility and grid size.
- [x] Display temporary alignment guides while moving elements.
- [x] Add rotation handles and precise rotation values.
- [x] Add bring forward, send backward, bring to front, and send to back.
- [x] Allow elements to be locked and hidden.

## Styling

- [x] Add fill-color selection.
- [x] Add border and stroke-color selection.
- [x] Add opacity controls.
- [x] Add configurable corner radius.
- [x] Add solid, dashed, and dotted stroke styles.
- [x] Add vertical text alignment.
- [x] Add text line-spacing controls.
- [x] Add letter-spacing controls.
- [x] Add automatic text fitting and overflow indicators.

## Elements

- [x] Add an image/logo element.
- [x] Add more basic shapes as required.

## Barcode and QR options

- [ ] Support Code 128, EAN-13, UPC, and other required barcode formats.
- [ ] Validate barcode content for the selected format.
- [ ] Allow barcode human-readable labels to be shown or hidden.
- [ ] Add barcode quiet-zone controls.
- [ ] Add QR error-correction settings.
- [ ] Add QR margin controls.
- [ ] Add barcode and QR foreground/background colors.
- [ ] Allow a logo to be embedded in QR codes.

## Printing and export

- [x] Add printer margin and X/Y offset calibration.
- [x] Export labels to PNG.
- [x] Export labels to PDF.

## Localization

- [x] Add Turkish language support.

## Reliability and document workflow

- [x] Warn before closing or replacing a document with unsaved changes.
- [ ] Add autosave and crash recovery.
- [x] Add a recent-files list.
- [ ] Validate imported template files and report actionable errors.
- [ ] Add automated tests for editing, serialization, rendering, and printing behavior.
