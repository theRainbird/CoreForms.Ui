namespace OldSchoolForms.Ui.Controls.Basic;

/// <summary>
/// Represents a column in a multi-column ComboBox dropdown.
/// </summary>
public class ComboBoxColumn
{
    /// <summary>
    /// Gets or sets the header text displayed in the column header.
    /// </summary>
    public string HeaderText { get; set; } = "";

    /// <summary>
    /// Gets or sets the width of the column in pixels.
    /// </summary>
    public int Width { get; set; } = 100;

    /// <summary>
    /// Gets or sets the property name on the data source item to bind to.
    /// When null, the column shows an empty cell.
    /// </summary>
    public string? DataPropertyName { get; set; }

    /// <summary>
    /// Gets or sets the horizontal alignment of cell content.
    /// </summary>
    public Advanced.DataGridViewContentAlignment TextAlign { get; set; } = Advanced.DataGridViewContentAlignment.Left;

    /// <summary>
    /// Gets or sets the format string applied to cell values (e.g. "N2", "d", "C").
    /// When null, ToString() is used.
    /// </summary>
    public string? FormatString { get; set; }
}
