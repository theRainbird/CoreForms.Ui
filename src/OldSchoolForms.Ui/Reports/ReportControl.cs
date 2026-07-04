using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Rendering;

namespace OldSchoolForms.Ui.Reports;

/// <summary>
/// Abstract base class for all report controls (labels, text boxes, images, lines, etc.).
/// Positions and sizes are specified in centimeters via the <see cref="Cm"/> type.
/// </summary>
public abstract class ReportControl
{
    private Font? _font;

    /// <summary>
    /// Gets or sets the name of this control.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the X position (from the left edge of the band), in cm.
    /// </summary>
    public Cm Left { get; set; }

    /// <summary>
    /// Gets or sets the Y position (from the top of the band), in cm.
    /// </summary>
    public Cm Top { get; set; }

    /// <summary>
    /// Gets or sets the width of the control, in cm.
    /// </summary>
    public Cm Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the control, in cm.
    /// </summary>
    public Cm Height { get; set; }

    /// <summary>
    /// Gets or sets whether this control is visible in the rendered report.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the control can grow vertically to fit its content.
    /// When true, the band height expands to accommodate the measured content.
    /// </summary>
    public bool CanGrow { get; set; }

    /// <summary>
    /// Gets or sets whether the control can shrink vertically when content is smaller than the set height.
    /// </summary>
    public bool CanShrink { get; set; }

    /// <summary>
    /// Gets or sets the background color of the control.
    /// </summary>
    public Color BackColor { get; set; } = Color.Transparent;

    /// <summary>
    /// Gets or sets the foreground (text) color.
    /// </summary>
    public Color ForeColor { get; set; } = Color.Black;

    /// <summary>
    /// Gets or sets the font used for text rendering.
    /// If null, inherits the report's default font.
    /// </summary>
    public Font? Font
    {
        get => _font;
        set => _font = value;
    }

    /// <summary>
    /// Gets or sets the horizontal text alignment.
    /// </summary>
    public TextAlignment TextAlign { get; set; } = TextAlignment.Left;

    /// <summary>
    /// Gets or sets a format string for the value (e.g., "{0:C}", "dd/MM/yyyy").
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Gets the effective font for this control, falling back to the default report font.
    /// </summary>
    public Font EffectiveFont => _font ?? Core.Font.Default;

    /// <summary>
    /// Gets the right edge position of this control.
    /// </summary>
    public Cm Right => Left + Width;

    /// <summary>
    /// Gets the bottom edge position of this control.
    /// </summary>
    public Cm Bottom => Top + Height;

    /// <summary>
    /// Measures the actual content height needed to fully render this control's content.
    /// Returns the greater of <see cref="Height"/> and the content-measured height
    /// when <see cref="CanGrow"/> is true.
    /// </summary>
    /// <param name="context">The current render context.</param>
    /// <returns>The actual height needed in cm.</returns>
    public abstract Cm MeasureContent(ReportRenderContext context);

    /// <summary>
    /// Renders this control's content to the specified Graphics surface.
    /// Coordinates are converted from cm to pixels using the context's RenderDpi.
    /// </summary>
    /// <param name="g">The Graphics to render to.</param>
    /// <param name="context">The current render context with record data and DPI info.</param>
    /// <param name="actualTop">The actual top position in cm (may differ from Top when a band grows).</param>
    /// <param name="actualHeight">The actual height in cm (may differ from Height when CanGrow or CanShrink).</param>
    public abstract void Render(Graphics g, ReportRenderContext context, Cm actualTop, Cm actualHeight);

    /// <summary>
    /// Converts a Cm value to pixel float at the context's render DPI.
    /// </summary>
    protected float CmToPx(Cm value, ReportRenderContext context) => value.ToPixelF(context.RenderDpi);

    /// <summary>
    /// Draws the control's background if BackColor is not transparent.
    /// </summary>
    protected void DrawBackground(Graphics g, ReportRenderContext context, Cm actualTop, Cm actualHeight)
    {
        if (BackColor.A == 0) return;
        float x = CmToPx(Left, context);
        float y = CmToPx(actualTop, context);
        float w = CmToPx(Width, context);
        float h = CmToPx(actualHeight, context);
        g.FillRectangle(BackColor, x, y, w, h);
    }

    /// <summary>
    /// Gets the resolved text value with Format string applied.
    /// </summary>
    protected string FormatValue(object? value)
    {
        if (value == null) return string.Empty;
        if (!string.IsNullOrEmpty(Format))
        {
            string fmt = Format.Contains("{0") ? Format : "{0:" + Format + "}";
            try { return string.Format(fmt, value); }
            catch { return value.ToString() ?? string.Empty; }
        }
        return value.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Calculates the X pixel offset for text alignment within the control.
    /// </summary>
    protected float GetAlignedX(float textWidthPx, float controlWidthPx)
    {
        return TextAlign switch
        {
            TextAlignment.Center => (controlWidthPx - textWidthPx) / 2f,
            TextAlignment.Right => controlWidthPx - textWidthPx,
            _ => 0f
        };
    }

    /// <summary>
    /// Calculates line wrapping and returns the total text height in cm for the given text and font.
    /// </summary>
    protected Cm MeasureTextHeight(string text, Font font, ReportRenderContext context)
    {
        if (string.IsNullOrEmpty(text)) return Cm.Zero;

        float controlWidthPx = CmToPx(Width, context);
        if (controlWidthPx <= 0) return Cm.Zero;

        float fontSizePx = font.Size * context.RenderDpi / 72f;
        float lineHeightPx = fontSizePx * 1.2f;

        if (context.MeasureTextCallback == null)
        {
            float charWidth = fontSizePx * 0.5f;
            int charsPerLine = Math.Max(1, (int)(controlWidthPx / charWidth));
            int lines = Math.Max(1, (text.Length + charsPerLine - 1) / charsPerLine);
            return new Cm(lines * lineHeightPx * 2.54 / context.RenderDpi);
        }

        var (textWidth, _) = context.MeasureTextCallback(text, font, 1f);
        if (textWidth <= controlWidthPx)
            return new Cm(lineHeightPx * 2.54 / context.RenderDpi);

        string[] words = text.Split(' ');
        int numLines = 1;
        float currentLineWidth = 0;

        foreach (var word in words)
        {
            var (wordWidth, _) = context.MeasureTextCallback(word + " ", font, 1f);
            if (currentLineWidth + wordWidth > controlWidthPx)
            {
                numLines++;
                currentLineWidth = wordWidth;
            }
            else
            {
                currentLineWidth += wordWidth;
            }
        }

        float totalHeightPx = numLines * lineHeightPx;
        return new Cm(totalHeightPx * 2.54 / context.RenderDpi);
    }

    /// <summary>
    /// Draws text with word wrapping within the control bounds.
    /// </summary>
    protected void DrawWrappedText(Graphics g, string text, Font font, ReportRenderContext context, Cm actualTop, Cm actualHeight, Color? foreColor = null)
    {
        if (string.IsNullOrEmpty(text)) return;

        float x = CmToPx(Left, context);
        float y = CmToPx(actualTop, context);
        float w = CmToPx(Width, context);
        float h = CmToPx(actualHeight, context);

        float fontSizePx = font.Size * context.RenderDpi / 72f;
        float lineHeightPx = fontSizePx * 1.2f;
        Color color = foreColor ?? ForeColor;

        if (context.MeasureTextCallback == null)
        {
            g.DrawString(text, font, color, x, y);
            return;
        }

        var (textWidth, _) = context.MeasureTextCallback(text, font, 1f);
        if (textWidth <= w)
        {
            float textX = x + GetAlignedX(textWidth, w);
            float textY = y + (h - fontSizePx) / 2f;
            g.DrawString(text, font, color, textX, textY);
            return;
        }

        string[] words = text.Split(' ');
        var lineWords = new List<string>();
        float currentLineWidth = 0;
        float drawY = y + (h > lineHeightPx * 3 ? fontSizePx * 0.2f : (h - fontSizePx) / 2f);

        void FlushLine()
        {
            if (lineWords.Count == 0) return;
            string line = string.Join(" ", lineWords);
            var (lineW, _) = context.MeasureTextCallback(line, font, 1f);
            float lineX = x + GetAlignedX(lineW, w);
            g.DrawString(line, font, color, lineX, drawY);
            drawY += lineHeightPx;
            lineWords.Clear();
            currentLineWidth = 0;
        }

        foreach (var word in words)
        {
            var (wordW, _) = context.MeasureTextCallback(word + " ", font, 1f);
            if (currentLineWidth + wordW > w && lineWords.Count > 0)
                FlushLine();
            lineWords.Add(word);
            currentLineWidth += wordW;
        }
        if (lineWords.Count > 0)
            FlushLine();
    }
}
