using CoreForms.Ui.Core;
using CoreForms.Ui.Rendering;

namespace CoreForms.Ui.Reports;

/// <summary>
/// A report control that draws a horizontal or vertical line.
/// The start coordinate is (Left, Top) and end coordinate is (X2, Y2), relative to the band origin in cm.
/// </summary>
public class ReportLine : ReportControl
{

    /// <summary>
    /// Gets or sets the ending X coordinate of the line, in cm.
    /// </summary>
    public Cm X2 { get; set; }

    /// <summary>
    /// Gets or sets the ending Y coordinate of the line (relative to band top), in cm.
    /// </summary>
    public Cm Y2 { get; set; }

    /// <summary>
    /// Gets or sets the line width in cm.
    /// </summary>
    public Cm LineWidth { get; set; } = 0.02;

    /// <summary>
    /// Gets or sets the line color.
    /// </summary>
    public Color LineColor { get; set; } = Color.Black;

    /// <summary>
    /// Returns Height (lines have fixed, non-growing content).
    /// </summary>
    public override Cm MeasureContent(ReportRenderContext context)
    {
        return Height;
    }

    /// <summary>
    /// Renders the line to the specified graphics surface.
    /// </summary>
    public override void Render(Graphics g, ReportRenderContext context, Cm actualTop, Cm actualHeight)
    {
        if (!Visible) return;

        float x1 = CmToPx(Left, context);
        float y1 = CmToPx(Top + (actualTop - Top), context);
        float x2 = CmToPx(X2, context);
        float y2 = CmToPx(Y2 + (actualTop - Top), context);
        float lw = CmToPx(LineWidth, context);

        g.DrawLine(LineColor, x1, y1, x2, y2, Math.Max(1f, lw));
    }
}
