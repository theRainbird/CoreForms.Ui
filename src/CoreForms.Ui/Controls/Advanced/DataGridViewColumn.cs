namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Represents a column in a DataGridView.
/// </summary>
public class DataGridViewColumn
{
    /// <summary>
    /// Gets or sets the name of the column.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the header text.
    /// </summary>
    public string HeaderText { get; set; } = "";

    /// <summary>
    /// Gets or sets the property name in the data source to bind to.
    /// </summary>
    public string DataPropertyName { get; set; } = "";

    /// <summary>
    /// Gets or sets the width of the column.
    /// </summary>
    public int Width { get; set; } = 100;

    /// <summary>
    /// Gets or sets the header cell.
    /// </summary>
    public DataGridViewHeaderCell? HeaderCell { get; set; }

    /// <summary>
    /// Gets or sets whether the column is read-only.
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Gets or sets the type of values in the column.
    /// </summary>
    public Type? ValueType { get; set; }
}