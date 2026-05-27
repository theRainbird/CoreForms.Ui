using CoreForms.Ui.Controls.Basic;

namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Specifies the type of editing control to use for a DataGridView column.
/// </summary>
public enum DataGridViewColumnEditType
{
    /// <summary>
    /// No editing is allowed; cells are display-only.
    /// </summary>
    None,

    /// <summary>
    /// Uses a <see cref="TextBox"/> as the cell editor.
    /// </summary>
    TextBox,

    /// <summary>
    /// Uses a <see cref="ComboBox"/> as the cell editor.
    /// </summary>
    ComboBox,

    /// <summary>
    /// Uses a <see cref="CheckBox"/> as the cell editor (toggles on click).
    /// </summary>
    CheckBox,

    /// <summary>
    /// Uses a <see cref="DateTimePicker"/> as the cell editor.
    /// </summary>
    DateTimePicker,
}

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
    public bool Groupable { get; set; } = true;

    /// <summary>
    /// Gets or sets the sort order for the column.
    /// </summary>
    public SortOrder SortOrder { get; set; } = SortOrder.None;

    /// <summary>
    /// Gets or sets the type of editing control used for cells in this column.
    /// </summary>
    public DataGridViewColumnEditType CellEditType { get; set; } = DataGridViewColumnEditType.None;

    /// <summary>
    /// Gets or sets the items displayed in the drop-down list when <see cref="CellEditType"/> is <see cref="DataGridViewColumnEditType.ComboBox"/>.
    /// </summary>
    public List<object>? Items { get; set; }

    /// <summary>
    /// Gets or sets the drop-down style when <see cref="CellEditType"/> is <see cref="DataGridViewColumnEditType.ComboBox"/>.
    /// </summary>
    public DropDownStyle ComboBoxDropDownStyle { get; set; } = DropDownStyle.DropDownList;

    /// <summary>
    /// Gets or sets the value that represents <c>true</c> when <see cref="CellEditType"/> is <see cref="DataGridViewColumnEditType.CheckBox"/>.
    /// When set, toggling the check box swaps between <see cref="TrueValue"/> and <see cref="FalseValue"/>.
    /// </summary>
    public object? TrueValue { get; set; }

    /// <summary>
    /// Gets or sets the value that represents <c>false</c> when <see cref="CellEditType"/> is <see cref="DataGridViewColumnEditType.CheckBox"/>.
    /// </summary>
    public object? FalseValue { get; set; }

    /// <summary>
    /// Gets or sets the format used by the <see cref="DateTimePicker"/> editor when <see cref="CellEditType"/> is <see cref="DataGridViewColumnEditType.DateTimePicker"/>.
    /// </summary>
    public DateTimePickerFormat PickerFormat { get; set; } = DateTimePickerFormat.Short;

    /// <summary>
    /// Gets or sets the custom format string when <see cref="PickerFormat"/> is <see cref="DateTimePickerFormat.Custom"/>.
    /// </summary>
    public string? PickerCustomFormat { get; set; }
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
