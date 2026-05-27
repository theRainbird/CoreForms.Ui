using CoreForms.Ui.Rendering;
using SkiaSharp;

namespace CoreForms.Ui.Reports;

/// <summary>
/// Exports a rendered report to PDF format using SkiaSharp.
/// The report is first rendered at 72 DPI (PDF point resolution) and then
/// written to a PDF document.
/// </summary>
public class ReportExportPdf
{
    /// <summary>
    /// Exports the specified report to a PDF file.
    /// </summary>
    /// <param name="report">The report definition to export.</param>
    /// <param name="filePath">The output PDF file path.</param>
    public void Export(Report report, string filePath)
    {
        byte[] bytes = ExportToBytes(report);
        File.WriteAllBytes(filePath, bytes);
    }

    /// <summary>
    /// Exports the specified report to a PDF byte array.
    /// </summary>
    /// <param name="report">The report definition to export.</param>
    /// <returns>A byte array containing the PDF data.</returns>
    public byte[] ExportToBytes(Report report)
    {
        var engine = new ReportRenderEngine();
        var pages = engine.Render(report, 72f);

        using var stream = new MemoryStream();
        using var pdfDoc = SKDocument.CreatePdf(stream);

        foreach (var page in pages)
        {
            float pageW = page.Width.ToPoints();
            float pageH = page.Height.ToPoints();

            using var canvas = pdfDoc.BeginPage(pageW, pageH);

            foreach (var cmd in page.Graphics.GetCommands())
            {
                RenderCommandToSkia(cmd, canvas);
            }

            pdfDoc.EndPage();
        }

        pdfDoc.Close();
        return stream.ToArray();
    }

    private static void RenderCommandToSkia(DrawCommand cmd, SKCanvas canvas)
    {
        switch (cmd.Type)
        {
            case DrawCommandType.FillRectangle:
                using (var paint = new SKPaint
                {
                    Color = ToSkColor(cmd.Color),
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                })
                {
                    canvas.DrawRect(cmd.X, cmd.Y, cmd.Width, cmd.Height, paint);
                }
                break;

            case DrawCommandType.DrawRectangle:
                using (var paint = new SKPaint
                {
                    Color = ToSkColor(cmd.Color),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = cmd.LineWidth > 0 ? cmd.LineWidth : 1,
                    IsAntialias = true
                })
                {
                    canvas.DrawRect(cmd.X, cmd.Y, cmd.Width, cmd.Height, paint);
                }
                break;

            case DrawCommandType.DrawLine:
                using (var paint = new SKPaint
                {
                    Color = ToSkColor(cmd.Color),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = cmd.LineWidth > 0 ? cmd.LineWidth : 1,
                    IsAntialias = true
                })
                {
                    canvas.DrawLine(cmd.X, cmd.Y, cmd.X2, cmd.Y2, paint);
                }
                break;

            case DrawCommandType.DrawString:
                if (!string.IsNullOrEmpty(cmd.Text) && cmd.Font != null)
                {
                    float fontSize = cmd.Font.Size * (cmd.Zoom > 0 ? cmd.Zoom : 1);
                    using var typeface = ResolveTypeface(cmd.Font.Name, cmd.Font.Style);

                    using var paint = new SKPaint
                    {
                        Typeface = typeface ?? SKTypeface.Default,
                        TextSize = fontSize,
                        Color = ToSkColor(cmd.Color),
                        IsAntialias = true,
                        FakeBoldText = cmd.Font.Style.HasFlag(Core.FontStyle.Bold)
                    };

                    bool fakeItalic = cmd.Font.Style.HasFlag(Core.FontStyle.Italic) &&
                                     (typeface == null || !typeface.IsItalic);

                    if (fakeItalic)
                    {
                        canvas.Save();
                        canvas.Skew(-0.2f, 0);
                        canvas.DrawText(cmd.Text, cmd.X, cmd.Y + fontSize, paint);
                        canvas.Restore();
                    }
                    else
                    {
                        canvas.DrawText(cmd.Text, cmd.X, cmd.Y + fontSize, paint);
                    }

                    if (cmd.Font.Style.HasFlag(Core.FontStyle.Underline))
                    {
                        float underlineY = cmd.Y + fontSize + 2;
                        using var linePaint = new SKPaint
                        {
                            Color = ToSkColor(cmd.Color),
                            Style = SKPaintStyle.Stroke,
                            StrokeWidth = 1
                        };
                        float textWidth = paint.MeasureText(cmd.Text);
                        canvas.DrawLine(cmd.X, underlineY, cmd.X + textWidth, underlineY, linePaint);
                    }

                    if (cmd.Font.Style.HasFlag(Core.FontStyle.Strikeout))
                    {
                        float strikeY = cmd.Y + fontSize * 0.55f;
                        using var linePaint = new SKPaint
                        {
                            Color = ToSkColor(cmd.Color),
                            Style = SKPaintStyle.Stroke,
                            StrokeWidth = 1
                        };
                        float textWidth = paint.MeasureText(cmd.Text);
                        canvas.DrawLine(cmd.X, strikeY, cmd.X + textWidth, strikeY, linePaint);
                    }
                }
                break;

            case DrawCommandType.FillEllipse:
                using (var paint = new SKPaint
                {
                    Color = ToSkColor(cmd.Color),
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                })
                {
                    canvas.DrawOval(cmd.X + cmd.Width / 2f, cmd.Y + cmd.Height / 2f,
                                    cmd.Width / 2f, cmd.Height / 2f, paint);
                }
                break;

            case DrawCommandType.DrawEllipse:
                using (var paint = new SKPaint
                {
                    Color = ToSkColor(cmd.Color),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = cmd.LineWidth > 0 ? cmd.LineWidth : 1,
                    IsAntialias = true
                })
                {
                    canvas.DrawOval(cmd.X + cmd.Width / 2f, cmd.Y + cmd.Height / 2f,
                                    cmd.Width / 2f, cmd.Height / 2f, paint);
                }
                break;

            case DrawCommandType.FillTriangle:
                using (var path = new SKPath())
                {
                    path.MoveTo(cmd.X, cmd.Y);
                    path.LineTo(cmd.X2, cmd.Y2);
                    path.LineTo(cmd.X3, cmd.Y3);
                    path.Close();
                    using var fillPaint = new SKPaint
                    {
                        Color = ToSkColor(cmd.Color),
                        Style = SKPaintStyle.Fill,
                        IsAntialias = true
                    };
                    canvas.DrawPath(path, fillPaint);
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
                    using var polyPath = new SKPath();
                    polyPath.MoveTo(cmd.Points[0], cmd.Points[1]);
                    for (int i = 2; i < cmd.Points.Length; i += 2)
                        polyPath.LineTo(cmd.Points[i], cmd.Points[i + 1]);
                    polyPath.Close();
                    using var fillP = new SKPaint { Color = ToSkColor(cmd.Color), IsAntialias = true, Style = SKPaintStyle.Fill };
                    canvas.DrawPath(polyPath, fillP);
                }
                break;

            case DrawCommandType.DrawPolygon:
                if (cmd.Points != null && cmd.Points.Length >= 2)
                {
                    using var polyPath = new SKPath();
                    polyPath.MoveTo(cmd.Points[0], cmd.Points[1]);
                    for (int i = 2; i < cmd.Points.Length; i += 2)
                        polyPath.LineTo(cmd.Points[i], cmd.Points[i + 1]);
                    using var drawP = new SKPaint { Color = ToSkColor(cmd.Color), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = cmd.LineWidth > 0 ? cmd.LineWidth : 1 };
                    canvas.DrawPath(polyPath, drawP);
                }
                break;

            case DrawCommandType.DrawImage:
                if (cmd.Image?.NativeImage != null)
                {
                    using var paint = new SKPaint { FilterQuality = SKFilterQuality.Low };
                    canvas.DrawImage(cmd.Image.NativeImage,
                        new SKRect(cmd.X, cmd.Y, cmd.X + cmd.Width, cmd.Y + cmd.Height), paint);
                }
                break;
        }
    }

    private static SKTypeface? ResolveTypeface(string fontName, Core.FontStyle style)
    {
        try
        {
            SKFontStyleSlant slant = style.HasFlag(Core.FontStyle.Italic)
                ? SKFontStyleSlant.Italic
                : SKFontStyleSlant.Upright;

            SKFontStyleWeight weight = style.HasFlag(Core.FontStyle.Bold)
                ? SKFontStyleWeight.Bold
                : SKFontStyleWeight.Normal;

            return SKTypeface.FromFamilyName(fontName, weight, SKFontStyleWidth.Normal, slant);
        }
        catch
        {
            return null;
        }
    }

    private static SKColor ToSkColor(Core.Color c) => new(c.R, c.G, c.B, c.A);
}
