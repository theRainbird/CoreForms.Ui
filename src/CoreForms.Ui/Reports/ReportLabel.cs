using CoreForms.Ui.Core;
using CoreForms.Ui.Rendering;

namespace CoreForms.Ui.Reports;

/// <summary>
/// A report control that displays static text.
/// </summary>
public class ReportLabel : ReportControl
{
    private string _text = string.Empty;

    /// <summary>
    /// Gets or sets the static text to display.
    /// </summary>
    public string Text
    {
        get => _text;
        set => _text = value ?? string.Empty;
    }

    /// <summary>
    /// Measures the content height of the label text.
    /// </summary>
    public override Cm MeasureContent(ReportRenderContext context)
    {
        if (string.IsNullOrEmpty(_text)) return Height;

        Cm textHeight = MeasureTextHeight(_text, EffectiveFont, context);
        return Cm.Max(Height, textHeight);
    }

    /// <summary>
    /// Renders the label text to the specified graphics surface.
    /// </summary>
    public override void Render(Graphics g, ReportRenderContext context, Cm actualTop, Cm actualHeight)
    {
        if (!Visible) return;

        DrawBackground(g, context, actualTop, actualHeight);
        DrawWrappedText(g, _text, EffectiveFont, context, actualTop, actualHeight);
    }
}
