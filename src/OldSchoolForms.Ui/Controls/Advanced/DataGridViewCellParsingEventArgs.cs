namespace OldSchoolForms.Ui.Controls.Advanced;

/// <summary>
/// Provides data for the <see cref="DataGridView.CellParsing"/> event.
/// Allows custom parsing of editor values back to the cell value.
/// </summary>
public class DataGridViewCellParsingEventArgs : DataGridViewCellEventArgs
{
    /// <summary>
    /// Gets or sets the parsed value to be stored in the cell.
    /// Set this property to override the default conversion.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets the raw value from the editing control.
    /// </summary>
    public object? RawValue { get; }

    /// <summary>
    /// Gets the desired type of the cell value.
    /// </summary>
    public Type? DesiredType { get; }

    /// <summary>
    /// Gets or sets whether custom parsing has been applied.
    /// Set to true after modifying <see cref="Value"/> to indicate that no further conversion is needed.
    /// </summary>
    public bool ParsingApplied { get; set; }

    /// <summary>
    /// Initializes a new instance of DataGridViewCellParsingEventArgs.
    /// </summary>
    /// <param name="columnIndex">The column index of the cell.</param>
    /// <param name="rowIndex">The row index of the cell.</param>
    /// <param name="rawValue">The raw value from the editing control.</param>
    /// <param name="desiredType">The desired type for the cell value, or null if unknown.</param>
    public DataGridViewCellParsingEventArgs(int columnIndex, int rowIndex, object? rawValue, Type? desiredType)
        : base(columnIndex, rowIndex)
    {
        RawValue = rawValue;
        Value = rawValue;
        DesiredType = desiredType;
    }
}
