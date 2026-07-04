namespace OldSchoolForms.Ui.Controls.Advanced;

/// <summary>
/// Provides data for cell click events.
/// </summary>
public class DataGridViewCellEventArgs : EventArgs
{
    /// <summary>
    /// Gets the column index of the cell.
    /// </summary>
    public int ColumnIndex { get; }

    /// <summary>
    /// Gets the row index of the cell.
    /// </summary>
    public int RowIndex { get; }

    /// <summary>
    /// Initializes a new instance of DataGridViewCellEventArgs.
    /// </summary>
    /// <param name="columnIndex">The column index.</param>
    /// <param name="rowIndex">The row index.</param>
    public DataGridViewCellEventArgs(int columnIndex, int rowIndex)
    {
        ColumnIndex = columnIndex;
        RowIndex = rowIndex;
    }
}