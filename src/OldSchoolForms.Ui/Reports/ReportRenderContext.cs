using OldSchoolForms.Ui.Rendering;

namespace OldSchoolForms.Ui.Reports;

/// <summary>
/// Provides contextual information during report rendering, including
/// the current record, page state, aggregation variables, and render resolution.
/// </summary>
public class ReportRenderContext
{
    /// <summary>
    /// Gets the report definition being rendered.
    /// </summary>
    public Report Report { get; }

    /// <summary>
    /// Gets or sets the current data record being processed.
    /// </summary>
    public object? CurrentRecord { get; set; }

    /// <summary>
    /// Gets or sets the zero-based index of the current record.
    /// </summary>
    public int CurrentRowIndex { get; set; }

    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets or sets the current group (when processing grouped reports).
    /// </summary>
    public ReportGroup? CurrentGroup { get; set; }

    /// <summary>
    /// Gets or sets the field value of the current group.
    /// Set by the render engine before rendering group headers/footers.
    /// Used by <see cref="ReportCrossTab"/> to filter records.
    /// </summary>
    public object? CurrentGroupValue { get; set; }

    /// <summary>
    /// Dictionary of named variables for aggregations (sum, count, etc.).
    /// </summary>
    public Dictionary<string, object> Variables { get; } = new();

    /// <summary>
    /// Gets or sets the render resolution in DPI.
    /// 96 for screen preview, 72 for PDF export.
    /// </summary>
    public float RenderDpi { get; set; } = 96f;

    /// <summary>
    /// Gets the Graphics.MeasureText callback, if available.
    /// </summary>
    public Graphics.MeasureTextCallback? MeasureTextCallback { get; set; }

    /// <summary>
    /// Initializes a new render context for the given report.
    /// </summary>
    public ReportRenderContext(Report report)
    {
        Report = report;
    }

    /// <summary>
    /// Gets the value of a named field from the current record using reflection.
    /// Supports special variables: PageNumber, TotalPages, CurrentRowIndex.
    /// </summary>
    /// <param name="fieldName">The name of the field or property.</param>
    /// <returns>The field value, or null if not found.</returns>
    public object? GetFieldValue(string fieldName)
    {
        if (string.Equals(fieldName, "PageNumber", StringComparison.OrdinalIgnoreCase))
            return PageNumber;
        if (string.Equals(fieldName, "TotalPages", StringComparison.OrdinalIgnoreCase))
            return TotalPages;
        if (string.Equals(fieldName, "CurrentRowIndex", StringComparison.OrdinalIgnoreCase))
            return CurrentRowIndex;

        if (CurrentRecord == null) return null;

        var type = CurrentRecord.GetType();
        var prop = type.GetProperty(fieldName);
        if (prop != null)
            return prop.GetValue(CurrentRecord);

        var field = type.GetField(fieldName);
        if (field != null)
            return field.GetValue(CurrentRecord);

        if (CurrentRecord is System.Collections.IDictionary dict)
            return dict[fieldName];

        return null;
    }
}
