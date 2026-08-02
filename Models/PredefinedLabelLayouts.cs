namespace wLabelDesigner.Models;

public static class PredefinedLabelLayouts
{
    public static IReadOnlyList<PredefinedLabelLayout> All { get; } =
    [
        new(
            "Cheese pallet · Standard",
            "100 × 100 mm pallet label with destination, carton count, lot, weight, and SSCC.",
            100,
            100,
            203,
            CreateStandardCheesePalletLabel),
        new(
            "Cheese pallet · Export",
            "Export shipping layout with consignee, origin, dates, carton count, and pallet ID.",
            100,
            100,
            203,
            CreateExportCheesePalletLabel),
        new(
            "Cheese pallet · Cold chain",
            "Cold-storage pallet layout with handling notice, destination, cartons, lot, and QR traceability.",
            100,
            100,
            203,
            CreateColdChainCheesePalletLabel),
        new(
            "Shipping label",
            "Recipient, address, tracking barcode, and order reference.",
            100,
            150,
            203,
            CreateShippingLabel),
        new(
            "Product label",
            "Compact product name, price, SKU, and barcode layout.",
            50,
            30,
            203,
            CreateProductLabel),
        new(
            "Shelf label",
            "Wide shelf tag with product details and a scannable barcode.",
            100,
            50,
            203,
            CreateShelfLabel),
        new(
            "QR contact card",
            "Name and contact details paired with a prominent QR code.",
            60,
            40,
            300,
            CreateQrContactCard)
    ];

    private static LabelDocument CreateStandardCheesePalletLabel() => new()
    {
        Name = "Cheese pallet · Standard",
        WidthMillimeters = 100,
        HeightMillimeters = 100,
        PrinterDpi = 203,
        Elements =
        [
            Text("YOUR COMPANY NAME", 4, 4, 92, 8, 16, bold: true, alignment: TextAlignmentOption.Left),
            Line(4, 13, 92),
            Text("SHIP TO", 4, 16, 19, 6, 9, bold: true, alignment: TextAlignmentOption.Left),
            Text("Customer / warehouse name\nStreet, city, postal code\nCountry", 24, 16, 72, 18, 10, alignment: TextAlignmentOption.Left),
            Line(4, 36, 92),
            Text("CHEESE PRODUCT NAME", 4, 39, 58, 8, 13, bold: true, alignment: TextAlignmentOption.Left),
            Text("CARTONS\n48", 65, 39, 31, 17, 15, bold: true),
            Text("LOT: LOT-0001\nNET WT: 480 kg\nPALLET: 1 OF 1", 4, 49, 58, 18, 10, alignment: TextAlignmentOption.Left),
            Barcode("003761234567890123", 9, 70, 82, 17),
            Text("SSCC 003761234567890123", 4, 88, 92, 6, 9),
            Rectangle(2, 2, 96, 96, 1.2)
        ]
    };

    private static LabelDocument CreateExportCheesePalletLabel() => new()
    {
        Name = "Cheese pallet · Export",
        WidthMillimeters = 100,
        HeightMillimeters = 100,
        PrinterDpi = 203,
        Elements =
        [
            Text("YOUR COMPANY NAME · EXPORT", 4, 4, 92, 8, 15, bold: true, alignment: TextAlignmentOption.Left),
            Line(4, 13, 92),
            Text("CONSIGNEE / DESTINATION", 4, 16, 92, 6, 9, bold: true, alignment: TextAlignmentOption.Left),
            Text("Importer or distribution center\nStreet, city, postal code · COUNTRY", 4, 23, 92, 13, 10, alignment: TextAlignmentOption.Left),
            Line(4, 38, 92),
            Text("PRODUCT: CHEESE TYPE / FORMAT\nORIGIN: COUNTRY OF ORIGIN\nLOT: LOT-0001", 4, 41, 58, 18, 9, alignment: TextAlignmentOption.Left),
            Text("CARTONS\n60", 66, 41, 30, 17, 15, bold: true),
            Text("PRODUCED: 2026-01-01\nBEST BEFORE: 2026-12-31\nNET / GROSS: 600 / 625 kg", 4, 61, 92, 15, 9, alignment: TextAlignmentOption.Left),
            Barcode("003761234567890130", 9, 78, 82, 13),
            Text("PALLET ID / SSCC: 003761234567890130", 4, 92, 92, 5, 8),
            Rectangle(2, 2, 96, 96, 1.2)
        ]
    };

    private static LabelDocument CreateColdChainCheesePalletLabel() => new()
    {
        Name = "Cheese pallet · Cold chain",
        WidthMillimeters = 100,
        HeightMillimeters = 100,
        PrinterDpi = 203,
        Elements =
        [
            Text("YOUR COMPANY NAME", 4, 4, 92, 7, 14, bold: true, alignment: TextAlignmentOption.Left),
            Text("KEEP REFRIGERATED · +2 °C TO +6 °C", 4, 13, 92, 9, 12, bold: true),
            Rectangle(3, 12, 94, 11, 1.5),
            Text("DESTINATION", 4, 26, 22, 6, 9, bold: true, alignment: TextAlignmentOption.Left),
            Text("Customer / cold store\nStreet, city, postal code", 27, 26, 69, 13, 9, alignment: TextAlignmentOption.Left),
            Line(4, 41, 92),
            Text("CHEESE PRODUCT NAME\nLOT: LOT-0001\nPACK: 10 kg", 4, 44, 55, 18, 10, bold: true, alignment: TextAlignmentOption.Left),
            Text("CARTONS\n48", 63, 44, 25, 17, 15, bold: true),
            QrCode("PALLET:003761234567890147|LOT:LOT-0001|CARTONS:48", 76, 64, 20),
            Text("PALLET 1 OF 1\nNET WT: 480 kg\nDISPATCH: 2026-01-01", 4, 65, 68, 16, 9, alignment: TextAlignmentOption.Left),
            Barcode("003761234567890147", 8, 83, 64, 12),
            Text("SSCC 003761234567890147", 4, 95, 68, 3, 7),
            Rectangle(2, 2, 96, 96, 1.2)
        ]
    };

    private static LabelDocument CreateShippingLabel() => new()
    {
        Name = "Shipping label",
        WidthMillimeters = 100,
        HeightMillimeters = 150,
        PrinterDpi = 203,
        Elements =
        [
            Text("SHIP TO", 6, 7, 88, 10, 16, bold: true, alignment: TextAlignmentOption.Left),
            Text("Recipient name", 6, 21, 88, 10, 15, bold: true, alignment: TextAlignmentOption.Left),
            Text("Street address\nCity, postal code\nCountry", 6, 33, 88, 28, 12, alignment: TextAlignmentOption.Left),
            Line(6, 66, 88),
            Text("ORDER #10001", 6, 72, 88, 9, 11, alignment: TextAlignmentOption.Left),
            Barcode("123456789012", 10, 92, 80, 28),
            Text("1234 5678 9012", 6, 123, 88, 8, 10),
            Rectangle(4, 4, 92, 140, 1.2)
        ]
    };

    private static LabelDocument CreateProductLabel() => new()
    {
        Name = "Product label",
        WidthMillimeters = 50,
        HeightMillimeters = 30,
        PrinterDpi = 203,
        Elements =
        [
            Text("PRODUCT NAME", 2, 2, 27, 6, 10, bold: true, alignment: TextAlignmentOption.Left),
            Text("$19.99", 31, 2, 17, 7, 14, bold: true),
            Barcode("12345678", 3, 11, 44, 11),
            Text("SKU 12345678", 2, 23, 46, 5, 8)
        ]
    };

    private static LabelDocument CreateShelfLabel() => new()
    {
        Name = "Shelf label",
        WidthMillimeters = 100,
        HeightMillimeters = 50,
        PrinterDpi = 203,
        Elements =
        [
            Text("PRODUCT NAME", 4, 4, 58, 9, 16, bold: true, alignment: TextAlignmentOption.Left),
            Text("Variant · 500 g", 4, 14, 58, 7, 10, alignment: TextAlignmentOption.Left),
            Text("$9.99", 67, 4, 29, 15, 24, bold: true),
            Barcode("123456789012", 10, 27, 80, 15),
            Text("123456789012", 4, 43, 92, 5, 8)
        ]
    };

    private static LabelDocument CreateQrContactCard() => new()
    {
        Name = "QR contact card",
        WidthMillimeters = 60,
        HeightMillimeters = 40,
        PrinterDpi = 300,
        Elements =
        [
            Text("YOUR NAME", 3, 5, 31, 8, 14, bold: true, alignment: TextAlignmentOption.Left),
            Text("Role or company", 3, 14, 31, 6, 9, alignment: TextAlignmentOption.Left),
            Text("email@example.com\n+00 000 000 000", 3, 23, 31, 12, 8, alignment: TextAlignmentOption.Left),
            QrCode("https://example.com", 37, 7, 20)
        ]
    };

    private static LabelElementData Text(
        string content,
        double x,
        double y,
        double width,
        double height,
        double fontSize,
        bool bold = false,
        TextAlignmentOption alignment = TextAlignmentOption.Center) => new()
        {
            Kind = LabelElementKind.Text,
            Content = content,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            FontSize = fontSize,
            IsBold = bold,
            TextAlignment = alignment
        };

    private static LabelElementData Barcode(string content, double x, double y, double width, double height) => new()
    {
        Kind = LabelElementKind.Barcode,
        Content = content,
        X = x,
        Y = y,
        Width = width,
        Height = height
    };

    private static LabelElementData QrCode(string content, double x, double y, double size) => new()
    {
        Kind = LabelElementKind.QrCode,
        Content = content,
        X = x,
        Y = y,
        Width = size,
        Height = size
    };

    private static LabelElementData Line(double x, double y, double width) => new()
    {
        Kind = LabelElementKind.Line,
        X = x,
        Y = y,
        Width = width,
        Height = 0.5
    };

    private static LabelElementData Rectangle(double x, double y, double width, double height, double strokeThickness) => new()
    {
        Kind = LabelElementKind.Rectangle,
        X = x,
        Y = y,
        Width = width,
        Height = height,
        StrokeThickness = strokeThickness
    };
}
