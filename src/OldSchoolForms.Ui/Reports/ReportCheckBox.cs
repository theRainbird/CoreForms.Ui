using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Rendering;

namespace OldSchoolForms.Ui.Reports;

/// <summary>
/// A report control that displays a check box bound to a boolean data field.
/// Draws a box with a checkmark or an X depending on the field value.
/// </summary>
public class ReportCheckBox : ReportControl
{
    /// <summary>
    /// Gets or sets the name of the boolean data field.
    /// </summary>
    public string? DataField { get; set; }

    /// <summary>
    /// Gets or sets a caption text displayed next to the checkbox.
    /// </summary>
    public string? Caption { get; set; }

    /// <summary>
    /// Gets the checked state based on the current record.
    /// </summary>
    public bool IsChecked
    {
        get
        {
            if (currentContext == null || string.IsNullOrEmpty(DataField))
                return false;
            var val = currentContext.GetFieldValue(DataField);
            return val is bool b && b;
        }
    }

    private ReportRenderContext? currentContext;

    /// <summary>
    /// Measures the content height (fixed height).
    /// </summary>
    public override Cm MeasureContent(ReportRenderContext context)
    {
        return Height;
    }

    /// <summary>
    /// Renders the checkbox to the specified graphics surface.
    /// </summary>
    public override void Render(Graphics g, ReportRenderContext context, Cm actualTop, Cm actualHeight)
    {
        if (!Visible) return;
        currentContext = context;

        DrawBackground(g, context, actualTop, actualHeight);

        float x = CmToPx(Left, context);
        float y = CmToPx(actualTop, context);
        float h = CmToPx(actualHeight, context);
        float boxSize = Math.Min(h * 0.8f, CmToPx(Width, context));
        float boxX = x;
        float boxY = y + (h - boxSize) / 2f;

        g.DrawRectangle(ForeColor, boxX, boxY, boxSize, boxSize, 1f);

        if (IsChecked)
        {
            float margin = boxSize * 0.2f;
            g.DrawLine(ForeColor, boxX + margin, boxY + margin, boxX + boxSize - margin, boxY + boxSize - margin, 1.5f);
            g.DrawLine(ForeColor, boxX + boxSize - margin, boxY + margin, boxX + margin, boxY + boxSize - margin, 1.5f);
        }

        if (!string.IsNullOrEmpty(Caption))
        {
            float captionX = boxX + boxSize + CmToPx(0.2, context);
            float captionY = y + (h - EffectiveFont.Size * context.RenderDpi / 72f) / 2f;
            g.DrawString(Caption, EffectiveFont, ForeColor, captionX, captionY);
        }
    }
}
