namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Defines how a field is used in a PivotTable.
/// </summary>
public enum PivotTableFieldUsage
{
    /// <summary>
    /// Field appears as a row header dimension.
    /// </summary>
    RowField,

    /// <summary>
    /// Field appears as a column header dimension.
    /// </summary>
    ColumnField,

    /// <summary>
    /// Field provides aggregated values in data cells.
    /// </summary>
    ValueField,

    /// <summary>
    /// Field filters the records before aggregation.
    /// </summary>
    FilterField
}

/// <summary>
/// Specifies the aggregation function for a value field.
/// </summary>
public enum PivotTableAggregation
{
    /// <summary>
    /// No aggregation (display raw values, requires unique combinations).
    /// </summary>
    None,

    /// <summary>
    /// Sum of values.
    /// </summary>
    Sum,

    /// <summary>
    /// Count of records.
    /// </summary>
    Count,

    /// <summary>
    /// Average of values.
    /// </summary>
    Average,

    /// <summary>
    /// Minimum value.
    /// </summary>
    Min,

    /// <summary>
    /// Maximum value.
    /// </summary>
    Max
}

/// <summary>
/// Specifies the visual layout style of the PivotTable.
/// </summary>
public enum PivotTableLayoutMode
{
    /// <summary>
    /// Compact layout: row fields are nested in a single column with indentation.
    /// </summary>
    Compact,

    /// <summary>
    /// Outline layout: each row field level has its own column with subtotals above groups.
    /// </summary>
    Outline,

    /// <summary>
    /// Tabular layout: each row field level has its own column, no indentation.
    /// </summary>
    Tabular
}

/// <summary>
/// Specifies which totals to show in the PivotTable.
/// </summary>
[Flags]
public enum PivotTableTotalVisibility
{
    /// <summary>
    /// No totals displayed.
    /// </summary>
    None = 0,

    /// <summary>
    /// Show row grand total.
    /// </summary>
    RowGrandTotal = 1,

    /// <summary>
    /// Show column grand total.
    /// </summary>
    ColumnGrandTotal = 2,

    /// <summary>
    /// Show subtotals at each group level.
    /// </summary>
    Subtotals = 4,

    /// <summary>
    /// Show all totals.
    /// </summary>
    All = RowGrandTotal | ColumnGrandTotal | Subtotals
}
