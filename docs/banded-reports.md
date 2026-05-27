# Banded Reports

CoreForms.Ui includes a banded report engine inspired by Microsoft Access Reports and SQL Server Reporting Services (RDLC). Reports are built programmatically in C# using bands (headers, detail, footers), support data binding, grouping, pagination, and can be previewed, printed, or exported to PDF.

All measurements use **centimeters** (`Cm` struct) — a metric unit system independent of screen DPI.

---

## Quick Start

```csharp
using CoreForms.Ui.Reports;

// 1. Create a report
var report = new Report("Employee List");
report.DataSource = employees; // any IEnumerable

// 2. Design bands
report.PageHeader.Height = 1.5;
report.PageHeader.Controls.Add(new ReportLabel
{
    Text = "Employee Report",
    Left = 0, Top = 0.2, Width = 17, Height = 1.0,
    Font = new Font("Arial", 16, FontStyle.Bold)
});

report.Detail.Height = 0.6;
report.Detail.Controls.Add(new ReportTextBox
{
    DataField = "Name",
    Left = 0, Top = 0, Width = 8, Height = 0.5
});

// 3. Preview
var viewer = new ReportViewer { Report = report, Dock = DockStyle.Fill };
viewer.RefreshReport();
form.Controls.Add(viewer);
```

---

## Page Setup

The `ReportPageSetup` class configures paper size, orientation, and margins.

### German Defaults

| Property | Default (Germany) |
|---|---|
| Paper | A4 (21.0 × 29.7 cm) |
| Orientation | Portrait |
| Margins | 2.0 cm all sides |

### Standard Paper Sizes

```csharp
report.PageSetup.SetPaperSize(ReportPaperSize.A4);                   // 21.0 × 29.7 cm
report.PageSetup.SetPaperSize(ReportPaperSize.A4, landscape: true); // 29.7 × 21.0 cm
report.PageSetup.SetPaperSize(ReportPaperSize.A3);                   // 29.7 × 42.0 cm
report.PageSetup.SetPaperSize(ReportPaperSize.A5);                   // 14.8 × 21.0 cm
report.PageSetup.SetPaperSize(ReportPaperSize.Letter);               // 21.59 × 27.94 cm
report.PageSetup.SetPaperSize(ReportPaperSize.Legal);                // 21.59 × 35.56 cm
```

### Orientation

```csharp
report.PageSetup.Landscape = true;   // width and height swap automatically
```

### Margins

```csharp
report.PageSetup.SetMargins(2.0);                           // 2 cm all sides
report.PageSetup.SetMargins(2.5, 2.0, 2.0, 2.0);          // left, right, top, bottom
report.PageSetup.LeftMargin = 2.5;                         // individual margin
report.PageSetup.TopMargin  = 2.0;
report.PageSetup.RightMargin = 2.0;
report.PageSetup.BottomMargin = 2.0;
```

### Existing Report Properties (Backward Compatible)

Older code that sets `PageWidth`, `PageHeight`, `LeftMargin`, etc. directly still works — these properties delegate to `PageSetup` internally.

```csharp
report.PageWidth  = 21.0;    // delegates to PageSetup
report.LeftMargin = 2.5;     // delegates to PageSetup
```

---

## Report Structure

A report consists of these bands, rendered in order:

```
┌─────────────────────────┐
│    ReportHeader         │  ← once at report start
├─────────────────────────┤
│    PageHeader           │  ← top of every page
├─────────────────────────┤
│   ┌───────────────────┐ │
│   │ GroupHeader       │ │  ← before each group (optional)
│   ├───────────────────┤ │
│   │ Detail            │ │  ← once per data record
│   ├───────────────────┤ │
│   │ GroupFooter       │ │  ← after each group (optional)
│   └───────────────────┘ │
├─────────────────────────┤
│    PageFooter           │  ← bottom of every page
├─────────────────────────┤
│    ReportFooter         │  ← once at report end
└─────────────────────────┘
```

```csharp
report.ReportHeader  // once at the beginning
report.PageHeader    // every page top
report.Detail        // every record
report.PageFooter    // every page bottom
report.ReportFooter  // once at the end
```

---

## Bands

Each `ReportBand` has:

| Property | Type | Description |
|---|---|---|
| `Height` | `Cm` | Band height |
| `BackColor` | `Color` | Background color (only within printable area) |
| `Visible` | `bool` | Show/hide the band |
| `CanGrow` | `bool` | Expand height for long content |
| `KeepTogether` | `bool` | Prevent page break within the band |
| `RepeatOnNewPage` | `bool` | Repeat header on each page (page/group headers) |
| `Controls` | `List<ReportControl>` | Controls in this band |

---

## Controls

All positions and sizes are in **centimeters**.

### ReportLabel — Static Text

```csharp
new ReportLabel
{
    Text = "Employee Name",
    Left = 0, Top = 0, Width = 6, Height = 0.5,
    Font = new Font("Arial", 10, FontStyle.Bold),
    ForeColor = Color.Black,
    TextAlign = TextAlignment.Left   // Left, Center, Right
};
```

### ReportTextBox — Data-Bound or Expression Text

```csharp
// Bound to a field
new ReportTextBox
{
    DataField = "LastName",
    Left = 0, Top = 0, Width = 6, Height = 0.5,
    Format = "{0:D3}",               // .NET format string (auto-wraps simple specifiers)
    TextAlign = TextAlignment.Right
};

// Expression with field replacement
new ReportTextBox
{
    Expression = "Total: {Salary:C}",
    Left = 0, Top = 0, Width = 6, Height = 0.5
};
```

### ReportCheckBox — Boolean Display

```csharp
new ReportCheckBox
{
    DataField = "IsActive",
    Caption = "Active",
    Left = 0, Top = 0, Width = 3, Height = 0.5
};
```

### ReportImage — Image

```csharp
new ReportImage
{
    Image = myImage,          // IGraphicsImage
    Sizing = ImageSizing.Zoom, // Normal, Stretch, Zoom
    Left = 0, Top = 0, Width = 5, Height = 3
};
```

### ReportLine — Horizontal/Vertical Rule

```csharp
new ReportLine
{
    Left = 0, Top = 0.5, X2 = 17, Y2 = 0.5,
    LineWidth = 0.02,
    LineColor = Color.FromArgb(180, 180, 180)
};
```

### Common Control Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Left` | `Cm` | 0 | X position relative to printable area |
| `Top` | `Cm` | 0 | Y position relative to band top |
| `Width` | `Cm` | 0 | Control width |
| `Height` | `Cm` | 0 | Control height |
| `Visible` | `bool` | true | Show/hide |
| `CanGrow` | `bool` | false | Expand height for content |
| `CanShrink` | `bool` | false | Shrink when content is shorter |
| `Font` | `Font` | inherit | Font or null to inherit |
| `ForeColor` | `Color` | Black | Text color |
| `BackColor` | `Color` | Transparent | Background color |
| `TextAlign` | `TextAlignment` | Left | Horizontal alignment |
| `Format` | `string` | null | .NET format string (e.g. `"{0:C}"`, `"{0:N0} EUR"`, or shorthand `"D3"`) |

---

## Grouping

Groups add header/footer bands that repeat when a field value changes.

```csharp
var group = new ReportGroup("Category", "CategoryGroup");
group.Header.Height = 0.5;
group.Header.Controls.Add(new ReportLabel
{
    Text = "Category:",
    Left = 0, Top = 0, Width = 4, Height = 0.5,
    Font = new Font("Arial", 10, FontStyle.Bold)
});
group.Footer.Height = 0.3;

report.Groups.Add(group);

// Nested groups
var subGroup = new ReportGroup("SubCategory");
group.Groups.Add(subGroup);
```

### Group Properties

| Property | Type | Description |
|---|---|---|
| `GroupField` | `string` | Data field to group on |
| `Header` | `ReportBand` | Rendered before each group |
| `Footer` | `ReportBand` | Rendered after each group |
| `KeepTogether` | `bool` | Keep entire group on one page |
| `Groups` | `ReportGroupCollection` | Nested child groups |

The render engine automatically sorts records by group fields. When a group value changes:
1. Inner group footers close (innermost first)
2. Outer group footers close
3. New group headers open (outermost first)
4. Inner group headers open

---

## The Cm Type (Centimeters)

All report measurements use the `Cm` struct instead of pixels.

```csharp
Cm width = 21.0;                    // implicit double → Cm
Cm height = Cm.FromMm(297);         // from millimeters
Cm inches = Cm.FromInches(8.5);     // from inches

Cm total = a + b;                   // arithmetic
bool fits = a < b;                  // comparison

int pixels = width.ToPixels(96);    // screen pixels at given DPI
float points = width.ToPoints();    // PDF points (1pt = 1/72 inch)
```

---

## Rendering and Pagination

The `ReportRenderEngine` handles:

- **Pagination**: automatic page breaks when content exceeds the page height
- **Margin handling**: bands start at `TopMargin`, footer ends at `BottomMargin`, content is offset by `LeftMargin` horizontally
- **KeepTogether**: prevents page breaks within a band
- **CanGrow**: bands expand to fit content
- **RepeatOnNewPage**: page/group headers repeat after page breaks
- **Two-pass rendering**: `{TotalPages}` is resolved correctly by first counting pages, then rendering with the actual total

```csharp
var engine = new ReportRenderEngine();
List<ReportPage> pages = engine.Render(report, renderDpi: 96);
```

---

## ReportViewer Control

The `ReportViewer` is a `UserControl` containing:

- **ToolStrip** (top): navigation buttons, page label, zoom, print, PDF export
- **StatusStrip** (bottom): page X of Y
- **ReportPreviewControl** (fill): page display with zoom, scrollbars, drop-shadow, and margin guides

### Properties

| Property | Type | Description |
|---|---|---|
| `Report` | `Report` | The report to preview |
| `CurrentPageIndex` | `int` | 0-based current page |
| `CurrentPage` | `int` | 1-based current page |
| `PageCount` | `int` | Total pages |
| `Zoom` | `float` | 0.25 – 4.0 |
| `ShowToolbar` | `bool` | Show/hide toolbar |
| `ShowStatusBar` | `bool` | Show/hide status bar |

### Methods

```csharp
viewer.RefreshReport();      // render the report
viewer.PrintReport();        // print via system dialog
viewer.ExportPdf("out.pdf"); // export to PDF file
viewer.FirstPage();          // navigate
viewer.PreviousPage();
viewer.NextPage();
viewer.LastPage();
```

### Events

| Event | Description |
|---|---|
| `PageChanged` | Current page or total page count changed |

### ReportPreviewControl (standalone)

Can be used without the toolbar/status bar:

```csharp
var preview = new ReportPreviewControl
{
    Report = report,
    Dock = DockStyle.Fill
};
preview.RefreshReport();
```

---

## PDF Export

```csharp
var exporter = new ReportExportPdf();
exporter.Export(report, "report.pdf");       // to file
byte[] data = exporter.ExportToBytes(report); // to byte array
```

The PDF exporter renders at 72 DPI (PDF points) using SkiaSharp. Text rendering uses the actual font via `SKTypeface.FromFamilyName` with bold/italic/strikeout/underline support.

---

## Full Example

```csharp
var report = new Report("Employee Report");
report.PageSetup.SetPaperSize(ReportPaperSize.A4, landscape: true);
report.PageSetup.SetMargins(1.5);

report.DataSource = new List<Person> { /* ... */ };

report.ReportHeader.Height = 2.0;
report.ReportHeader.Controls.Add(new ReportLabel
{
    Text = "Company Confidential",
    Font = new Font("Arial", 14, FontStyle.Bold),
    Left = 0, Top = 0.5, Width = 26.7, Height = 1.0,
    TextAlign = TextAlignment.Center
});

report.PageHeader.Height = 1.5;
report.PageHeader.Controls.Add(new ReportLabel
{
    Text = "Employee Report",
    Left = 0, Top = 0, Width = 26.7, Height = 1.0,
    Font = new Font("Arial", 16, FontStyle.Bold),
    TextAlign = TextAlignment.Center
});

report.Detail.Height = 0.5;
report.Detail.Controls.Add(new ReportTextBox
{
    DataField = "Name",
    Left = 0, Top = 0, Width = 10, Height = 0.45
});

report.PageFooter.Height = 0.5;
report.PageFooter.Controls.Add(new ReportTextBox
{
    Expression = "Page {PageNumber} / {TotalPages}",
    Left = 20, Top = 0, Width = 6.7, Height = 0.4,
    TextAlign = TextAlignment.Right,
    Font = new Font("Arial", 7), ForeColor = Color.Gray
});

var viewer = new ReportViewer { Report = report, Dock = DockStyle.Fill };
viewer.RefreshReport();
// add viewer to form...
```

---

## Architecture Notes

- All report controls live in the `CoreForms.Ui.Reports` namespace
- `Cm` struct is the unit for all measurements (converts to pixels at render time)
- Rendering uses the command-list pattern (`Graphics` + `SkiaRenderer`)
- The engine offsets all content by `LeftMargin` and `TopMargin` to keep content within the printable area
- Band backgrounds fill only the printable area, not the margins
- Page headers/footers repeat on each page; group headers/footers repeat on group boundaries
- Scrollbars appear automatically when zoomed in past the viewport size
