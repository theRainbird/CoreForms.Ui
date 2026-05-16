namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Represents a row in a DataGridView.
/// </summary>
public class DataGridViewRow
{
    /// <summary>
    /// Gets the collection of cells in the row.
    /// </summary>
    public List<DataGridViewCell> Cells { get; } = new();

    /// <summary>
    /// Gets or sets the data-bound object associated with this row.
    /// </summary>
    public object? DataBoundItem { get; set; }
}