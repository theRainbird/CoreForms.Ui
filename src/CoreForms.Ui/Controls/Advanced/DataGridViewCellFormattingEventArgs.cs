namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Provides data for the <see cref="DataGridView.CellFormatting"/> event.
/// Allows custom formatting of cell values for display.
/// </summary>
public class DataGridViewCellFormattingEventArgs : DataGridViewCellEventArgs
{
    /// <summary>
    /// Gets or sets the value to be displayed in the cell.
    /// Set this property to override the default formatted value.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets the raw, unformatted value from the data source.
    /// </summary>
    public object? RawValue { get; }

    /// <summary>
    /// Gets or sets the format string applied to the cell value.
    /// </summary>
    public string? FormatString { get; set; }

    /// <summary>
    /// Gets or sets whether custom formatting has been applied.
    /// Set to true after modifying <see cref="Value"/> to indicate that no further formatting is needed.
    /// </summary>
    public bool FormattingApplied { get; set; }

    /// <summary>
    /// Initializes a new instance of DataGridViewCellFormattingEventArgs.
    /// </summary>
    /// <param name="columnIndex">The column index of the cell.</param>
    /// <param name="rowIndex">The row index of the cell.</param>
    /// <param name="rawValue">The raw value from the data source.</param>
    /// <param name="formatString">The format string for the column.</param>
    public DataGridViewCellFormattingEventArgs(int columnIndex, int rowIndex, object? rawValue, string? formatString)
        : base(columnIndex, rowIndex)
    {
        RawValue = rawValue;
        Value = rawValue;
        FormatString = formatString;
    }
}
