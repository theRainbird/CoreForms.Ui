using OldSchoolForms.Ui.Core;

namespace OldSchoolForms.Ui.Controls.Advanced;

/// <summary>
/// Represents a header cell in a DataGridView column.
/// </summary>
public class DataGridViewHeaderCell
{
    /// <summary>
    /// Gets or sets the text of the header cell.
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// Gets or sets the font of the header cell.
    /// </summary>
    public Font? Font { get; set; }
}