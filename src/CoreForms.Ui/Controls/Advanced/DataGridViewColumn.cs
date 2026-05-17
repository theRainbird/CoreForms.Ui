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

    /// <summary>
    /// Gets or sets the horizontal alignment of cell content.
    /// </summary>
    public DataGridViewContentAlignment TextAlign { get; set; } = DataGridViewContentAlignment.Left;

    /// <summary>
    /// Gets or sets the format string applied to cell values (e.g. "N2", "d", "C").
    /// When null, ToString() is used.
    /// </summary>
    public string? FormatString { get; set; }

    /// <summary>
    /// Gets or sets whether the column width can be resized by the user at runtime.
    /// </summary>
    public bool Resizable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the column can be sorted by clicking the column header.
    /// </summary>
    public bool Sortable { get; set; } = true;

    /// <summary>
    /// Gets or sets the current sort order of the column.
    /// </summary>
    public SortOrder SortOrder { get; set; } = SortOrder.None;
}

/// <summary>
/// Specifies the sort order for a DataGridView column.
/// </summary>
public enum SortOrder
{
    /// <summary>
    /// No sorting is applied.
    /// </summary>
    None,

    /// <summary>
    /// Sorted in ascending order.
    /// </summary>
    Ascending,

    /// <summary>
    /// Sorted in descending order.
    /// </summary>
    Descending
}