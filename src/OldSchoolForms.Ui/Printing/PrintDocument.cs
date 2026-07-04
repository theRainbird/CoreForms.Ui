using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Rendering;
using SkiaSharp;

namespace OldSchoolForms.Ui.Printing;

/// <summary>
/// Defines a reusable object that sends output to a printer.
/// </summary>
public class PrintDocument : Component
{
    private string _documentName = "Document";

    /// <summary>
    /// Gets or sets the name of the document being printed.
    /// </summary>
    public string DocumentName
    {
        get => _documentName;
        set => _documentName = value ?? "Document";
    }

    /// <summary>
    /// Gets or sets the PrinterSettings for this document.
    /// </summary>
    public PrinterSettings PrinterSettings { get; set; } = new();

    /// <summary>
    /// Gets or sets the default page settings for this document.
    /// </summary>
    public PageSettings DefaultPageSettings { get; set; } = new();

    /// <summary>
    /// Occurs when the print job needs to render a page.
    /// </summary>
    public event EventHandler<PrintPageEventArgs>? PrintPage;

    /// <summary>
    /// Raises the PrintPage event.
    /// </summary>
    protected virtual void OnPrintPage(PrintPageEventArgs e) => PrintPage?.Invoke(this, e);

    /// <summary>
    /// Prints the document using the configured printer and page settings.
    /// Generates a PDF via SkiaSharp and sends it to the platform print subsystem.
    /// </summary>
    public void Print()
    {
        if (string.IsNullOrEmpty(PrinterSettings.PrinterName))
        {
            var printers = PrinterSettings.InstalledPrinters;
            if (printers.Length == 0)
                throw new InvalidOperationException("No printers installed.");
            PrinterSettings.PrinterName = printers[0];
        }

        using var pdfStream = new MemoryStream();
        using var pdfDoc = SKDocument.CreatePdf(pdfStream);

        var pageSettings = DefaultPageSettings.Clone();
        pageSettings.PrinterSettings = PrinterSettings;

        var pageBounds = pageSettings.Bounds;
        float scale = 72.0f / 100.0f;

        bool hasMorePages = true;
        int pageNumber = 0;

        while (hasMorePages)
        {
            float pageW = pageBounds.Width * scale;
            float pageH = pageBounds.Height * scale;

            var canvas = pdfDoc.BeginPage(pageW, pageH);
            if (canvas == null)
                break;

            using (canvas)
            {
                var marginBounds = pageSettings.PrintableBounds;

                var g = new Graphics();
                g.TranslateTransform(marginBounds.X * scale, marginBounds.Y * scale);
                g.MeasureText = (text, font, zoom) =>
                {
                    float fontSize = font.Size * zoom;
                    using var skPaint = new SKPaint
                    {
                        Typeface = SKTypeface.Default,
                        TextSize = fontSize,
                        IsAntialias = true
                    };
                    float skWidth = skPaint.MeasureText(text);
                    return ((int)skWidth, (int)fontSize);
                };

                var args = new PrintPageEventArgs(
                    g,
                    pageSettings,
                    new Rectangle(0, 0, (int)pageW, (int)pageH),
                    new Rectangle(
                        (int)(marginBounds.X * scale),
                        (int)(marginBounds.Y * scale),
                        (int)(marginBounds.Width * scale),
                        (int)(marginBounds.Height * scale)));

                OnPrintPage(args);
                hasMorePages = args.HasMorePages && !args.Cancel;
                pageNumber++;

                foreach (var cmd in g.GetCommands())
                {
                    RenderCommandToSkia(cmd, canvas);
                }
            }

            pdfDoc.EndPage();
        }

        pdfDoc.Close();
        pdfStream.Flush();

        byte[] pdfBytes = pdfStream.ToArray();
        Platform.PrintDialogImpl.PrintPdf(pdfBytes, PrinterSettings, _documentName);
    }

    private static void RenderCommandToSkia(DrawCommand cmd, SKCanvas canvas)
    {
        switch (cmd.Type)
        {
            case DrawCommandType.FillRectangle:
                using (var paint = new SKPaint { Color = ToSkColor(cmd.Color), Style = SKPaintStyle.Fill })
                    canvas.DrawRect(cmd.X, cmd.Y, cmd.Width, cmd.Height, paint);
                break;
            case DrawCommandType.DrawRectangle:
                using (var paint = new SKPaint { Color = ToSkColor(cmd.Color), Style = SKPaintStyle.Stroke, StrokeWidth = cmd.LineWidth > 0 ? cmd.LineWidth : 1 })
                    canvas.DrawRect(cmd.X, cmd.Y, cmd.Width, cmd.Height, paint);
                break;
            case DrawCommandType.DrawLine:
                using (var paint = new SKPaint { Color = ToSkColor(cmd.Color), Style = SKPaintStyle.Stroke, StrokeWidth = cmd.LineWidth > 0 ? cmd.LineWidth : 1 })
                    canvas.DrawLine(cmd.X, cmd.Y, cmd.X2, cmd.Y2, paint);
                break;
            case DrawCommandType.DrawString:
                if (!string.IsNullOrEmpty(cmd.Text) && cmd.Font != null)
                {
                    float fontSize = cmd.Font.Size * (cmd.Zoom > 0 ? cmd.Zoom : 1);
                    using var skPaint = new SKPaint
                    {
                        Typeface = SKTypeface.Default,
                        TextSize = fontSize,
                        Color = ToSkColor(cmd.Color),
                        IsAntialias = true
                    };
                    canvas.DrawText(cmd.Text, cmd.X, cmd.Y + fontSize, skPaint);
                }
                break;
            case DrawCommandType.FillTriangle:
                using (var path = new SKPath())
                {
                    path.MoveTo(cmd.X, cmd.Y);
                    path.LineTo(cmd.X2, cmd.Y2);
                    path.LineTo(cmd.X3, cmd.Y3);
                    path.Close();
                    using var paint = new SKPaint { Color = ToSkColor(cmd.Color), Style = SKPaintStyle.Fill };
                    canvas.DrawPath(path, paint);
                }
                break;
            case DrawCommandType.FillPie:
                using (var piePaint = new SKPaint { Color = ToSkColor(cmd.Color), IsAntialias = true, Style = SKPaintStyle.Fill })
                {
                    canvas.DrawArc(new SKRect(cmd.X, cmd.Y, cmd.X + cmd.Width, cmd.Y + cmd.Height), cmd.StartAngle, cmd.SweepAngle, true, piePaint);
                }
                break;
            case DrawCommandType.DrawArc:
                using (var arcPaint = new SKPaint { Color = ToSkColor(cmd.Color), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = cmd.LineWidth > 0 ? cmd.LineWidth : 1 })
                {
                    canvas.DrawArc(new SKRect(cmd.X, cmd.Y, cmd.X + cmd.Width, cmd.Y + cmd.Height), cmd.StartAngle, cmd.SweepAngle, false, arcPaint);
                }
                break;
            case DrawCommandType.FillPolygon:
                if (cmd.Points != null && cmd.Points.Length >= 2)
                {
                    using var fillPath = new SKPath();
                    fillPath.MoveTo(cmd.Points[0], cmd.Points[1]);
                    for (int i = 2; i < cmd.Points.Length; i += 2)
                        fillPath.LineTo(cmd.Points[i], cmd.Points[i + 1]);
                    fillPath.Close();
                    using var paint = new SKPaint { Color = ToSkColor(cmd.Color), Style = SKPaintStyle.Fill };
                    canvas.DrawPath(fillPath, paint);
                }
                break;
            case DrawCommandType.DrawPolygon:
                if (cmd.Points != null && cmd.Points.Length >= 2)
                {
                    using var drawPath = new SKPath();
                    drawPath.MoveTo(cmd.Points[0], cmd.Points[1]);
                    for (int i = 2; i < cmd.Points.Length; i += 2)
                        drawPath.LineTo(cmd.Points[i], cmd.Points[i + 1]);
                    using var paint = new SKPaint { Color = ToSkColor(cmd.Color), Style = SKPaintStyle.Stroke, StrokeWidth = cmd.LineWidth > 0 ? cmd.LineWidth : 1 };
                    canvas.DrawPath(drawPath, paint);
                }
                break;
            case DrawCommandType.FillEllipse:
                using (var paint = new SKPaint { Color = ToSkColor(cmd.Color), Style = SKPaintStyle.Fill })
                    canvas.DrawOval(cmd.X + cmd.Width / 2f, cmd.Y + cmd.Height / 2f, cmd.Width / 2f, cmd.Height / 2f, paint);
                break;
        }
    }

    private static SKColor ToSkColor(Core.Color c) => new(c.R, c.G, c.B, c.A);
}
