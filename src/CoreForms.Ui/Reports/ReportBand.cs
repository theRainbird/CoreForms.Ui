using CoreForms.Ui.Core;
using CoreForms.Ui.Rendering;

namespace CoreForms.Ui.Reports;

/// <summary>
/// Represents a report band section (header, detail, footer, etc.).
/// Each band contains a collection of <see cref="ReportControl"/> items
/// and has a height, visibility, and layout behavior.
/// </summary>
public class ReportBand
{
    private readonly List<ReportControl> _controls = new();

    /// <summary>
    /// Gets the type of this band (ReportHeader, PageHeader, Detail, etc.).
    /// </summary>
    public ReportBandType BandType { get; }

    /// <summary>
    /// Gets or sets the height of this band, in cm.
    /// May be exceeded when <see cref="CanGrow"/> is true.
    /// </summary>
    public Cm Height { get; set; }

    /// <summary>
    /// Gets or sets the background color of this band.
    /// </summary>
    public Color BackColor { get; set; } = Color.Transparent;

    /// <summary>
    /// Gets or sets whether this band is visible in the rendered report.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the band can grow vertically to accommodate
    /// controls with <c>CanGrow = true</c> that need more space.
    /// </summary>
    public bool CanGrow { get; set; }

    /// <summary>
    /// Gets or sets whether this band should repeat on each new page
    /// when a page break occurs within the data. Applies to page/group headers.
    /// </summary>
    public bool RepeatOnNewPage { get; set; }

    /// <summary>
    /// Gets or sets whether the band should be kept together on one page if possible.
    /// If the remaining page space is insufficient, a page break is inserted before this band.
    /// </summary>
    public bool KeepTogether { get; set; }

    /// <summary>
    /// Gets or sets the data field used for grouping (only relevant for GroupHeader/GroupFooter).
    /// </summary>
    public string? GroupField { get; set; }

    /// <summary>
    /// Gets the list of report controls within this band.
    /// </summary>
    public List<ReportControl> Controls => _controls;

    /// <summary>
    /// Initializes a new ReportBand of the specified type.
    /// </summary>
    /// <param name="bandType">The type of band.</param>
    public ReportBand(ReportBandType bandType)
    {
        BandType = bandType;
    }

    /// <summary>
    /// Measures the actual height needed by this band based on its controls.
    /// If <see cref="CanGrow"/> is true, returns the maximum of the nominal height
    /// and the tallest measured control content.
    /// </summary>
    /// <param name="context">The render context.</param>
    /// <returns>The actual height in cm.</returns>
    public Cm MeasureHeight(ReportRenderContext context)
    {
        if (!Visible || !_controls.Any(c => c.Visible))
            return Cm.Zero;

        if (!CanGrow)
            return Height;

        Cm maxContentHeight = Cm.Zero;
        foreach (var ctrl in _controls)
        {
            if (!ctrl.Visible) continue;
            Cm contentH = ctrl.MeasureContent(context);
            if (contentH > maxContentHeight)
                maxContentHeight = contentH;
        }

        return maxContentHeight > Height ? maxContentHeight : Height;
    }

    /// <summary>
    /// Renders all visible controls in this band.
    /// </summary>
    /// <param name="g">The Graphics surface to render to.</param>
    /// <param name="context">The render context.</param>
    /// <param name="bandOffsetY">The Y offset of this band within the page, in cm.</param>
    /// <param name="actualHeight">The actual rendered height of this band (may differ from Height if CanGrow).</param>
    public void Render(Graphics g, ReportRenderContext context, Cm bandOffsetY, Cm actualHeight)
    {
        if (!Visible || _controls.Count == 0) return;

        if (BackColor.A != 0)
        {
            float bgX = 0;
            float bgY = CmToPx(bandOffsetY, context);
            float bgW = CmToPx(context.Report.PageWidth, context);
            float bgH = CmToPx(actualHeight, context);
            g.FillRectangle(BackColor, bgX, bgY, bgW, bgH);
        }

        foreach (var ctrl in _controls)
        {
            if (!ctrl.Visible) continue;

            Cm ctrlContentHeight = ctrl.CanGrow || ctrl.CanShrink
                ? ctrl.MeasureContent(context)
                : ctrl.Height;

            if (ctrl.CanShrink && ctrlContentHeight < ctrl.Height)
                ctrlContentHeight = ctrlContentHeight > Cm.Zero ? ctrlContentHeight : ctrl.Height;
            else if (!ctrl.CanGrow && !ctrl.CanShrink)
                ctrlContentHeight = ctrl.Height;

            Cm ctrlTop = ctrl.Top + bandOffsetY;

            ctrl.Render(g, context, ctrlTop, ctrlContentHeight);
        }
    }

    private static float CmToPx(Cm value, ReportRenderContext context) => value.ToPixelF(context.RenderDpi);
}
