using CoreForms.Ui.Core;
using CoreForms.Ui.Rendering;

namespace CoreForms.Ui.Reports;

/// <summary>
/// A report control that displays data-bound text, computed expressions, or static values.
/// Field references in <see cref="Expression"/> use the format <c>{FieldName}</c>.
/// </summary>
public class ReportTextBox : ReportControl
{
    private string? _dataField;
    private string? _expression;

    /// <summary>
    /// Gets or sets the name of the data field to bind to from the current record.
    /// </summary>
    public string? DataField
    {
        get => _dataField;
        set => _dataField = value;
    }

    /// <summary>
    /// Gets or sets an expression template. Field references use <c>{FieldName}</c> syntax.
    /// Example: <c>"Total: {TotalAmount:C}"</c>
    /// When both DataField and Expression are set, Expression takes precedence.
    /// </summary>
    public string? Expression
    {
        get => _expression;
        set => _expression = value;
    }

    /// <summary>
    /// Gets the resolved text value from the current record.
    /// </summary>
    public string ResolvedValue
    {
        get
        {
            object? raw = null;

            if (!string.IsNullOrEmpty(_expression))
            {
                string result = _expression;
                int start = result.IndexOf('{');
                while (start >= 0)
                {
                    int end = result.IndexOf('}', start);
                    if (end < 0) break;
                    string fieldName = result.Substring(start + 1, end - start - 1);
                    object? fieldVal = null;
                    if (context != null)
                        fieldVal = context.GetFieldValue(fieldName);
                    string replacement = fieldVal?.ToString() ?? string.Empty;
                    result = result[..start] + replacement + result[(end + 1)..];
                    start = result.IndexOf('{', start + replacement.Length);
                }
                return result;
            }

            if (!string.IsNullOrEmpty(_dataField) && context != null)
                raw = context.GetFieldValue(_dataField);

            return FormatValue(raw);
        }
    }

    private ReportRenderContext? context;

    /// <summary>
    /// Measures the content height based on the resolved text value.
    /// </summary>
    public override Cm MeasureContent(ReportRenderContext ctx)
    {
        context = ctx;
        string text = ResolvedValue;
        if (string.IsNullOrEmpty(text)) return Height;

        Cm textHeight = MeasureTextHeight(text, EffectiveFont, ctx);
        return Cm.Max(Height, textHeight);
    }

    /// <summary>
    /// Renders the text box content to the specified graphics surface.
    /// </summary>
    public override void Render(Graphics g, ReportRenderContext ctx, Cm actualTop, Cm actualHeight)
    {
        if (!Visible) return;
        context = ctx;

        DrawBackground(g, ctx, actualTop, actualHeight);

        string text = ResolvedValue;
        if (!string.IsNullOrEmpty(text))
            DrawWrappedText(g, text, EffectiveFont, ctx, actualTop, actualHeight);
    }
}
