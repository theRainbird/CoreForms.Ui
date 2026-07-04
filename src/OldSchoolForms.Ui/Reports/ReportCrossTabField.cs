namespace OldSchoolForms.Ui.Reports;

/// <summary>
/// Defines how a field is used in a cross-tab report.
/// </summary>
public enum FieldUsage
{
    /// <summary>
    /// Field appears as a row header (one row per unique value).
    /// </summary>
    RowField,
    /// <summary>
    /// Field appears as a column header (one column per unique value).
    /// </summary>
    ColumnField,
    /// <summary>
    /// Field provides numeric values for aggregation in each cell.
    /// </summary>
    ValueField
}

/// <summary>
/// Defines the aggregation function applied to value fields in a cross-tab.
/// </summary>
public enum CrossTabAggregation
{
    /// <summary>Sum of all values.</summary>
    Sum,
    /// <summary>Count of non-null values.</summary>
    Count,
    /// <summary>Average of all values.</summary>
    Avg,
    /// <summary>Minimum value.</summary>
    Min,
    /// <summary>Maximum value.</summary>
    Max
}

/// <summary>
/// Defines a single field in a cross-tab report definition.
/// </summary>
public class CrossTabField
{
    /// <summary>
    /// Gets or sets the property or field name on the data record.
    /// </summary>
    public string DataField { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display header text. If null, <see cref="DataField"/> is used.
    /// </summary>
    public string? HeaderText { get; set; }

    /// <summary>
    /// Gets or sets how this field is used (row, column, or value).
    /// </summary>
    public FieldUsage Usage { get; set; } = FieldUsage.ValueField;

    /// <summary>
    /// Gets or sets the aggregation function. Only relevant for <see cref="FieldUsage.ValueField"/>.
    /// </summary>
    public CrossTabAggregation Aggregation { get; set; } = CrossTabAggregation.Sum;

    /// <summary>
    /// Gets or sets an optional .NET format string (e.g. "{0:N0}", "C2").
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Gets the effective header text.
    /// For value fields without an explicit <see cref="HeaderText"/>,
    /// appends the aggregation name (e.g. "Salary (Sum)").
    /// </summary>
    public string DisplayHeader
    {
        get
        {
            if (HeaderText != null) return HeaderText;
            if (Usage == FieldUsage.ValueField)
                return $"{DataField} ({Aggregation})";
            return DataField;
        }
    }
}
