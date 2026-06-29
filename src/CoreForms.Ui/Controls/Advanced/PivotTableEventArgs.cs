using CoreForms.Ui.Core;
using Graphics = CoreForms.Ui.Rendering.Graphics;

namespace CoreForms.Ui.Controls.Advanced;

/// <summary>
/// Provides data for the <see cref="PivotTable.CellFormatting"/> event.
/// Allows customization of the display value and formatting of a data cell.
/// </summary>
public class PivotTableCellFormattingEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="PivotTableCellFormattingEventArgs"/>.
    /// </summary>
    public PivotTableCellFormattingEventArgs(
        int rowIndex, int columnIndex,
        int rowFieldLevel, int columnFieldLevel,
        object? rawValue, string? formatString)
    {
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
        RowFieldLevel = rowFieldLevel;
        ColumnFieldLevel = columnFieldLevel;
        RawValue = rawValue;
        FormatString = formatString;
        FormattedValue = FormatValue(rawValue, formatString);
    }

    /// <summary>
    /// Gets the index of the row in the visible matrix.
    /// </summary>
    public int RowIndex { get; }

    /// <summary>
    /// Gets the index of the column in the visible matrix.
    /// </summary>
    public int ColumnIndex { get; }

    /// <summary>
    /// Gets the hierarchy depth of the row field.
    /// </summary>
    public int RowFieldLevel { get; }

    /// <summary>
    /// Gets the hierarchy depth of the column field.
    /// </summary>
    public int ColumnFieldLevel { get; }

    /// <summary>
    /// Gets the raw aggregated value.
    /// </summary>
    public object? RawValue { get; }

    /// <summary>
    /// Gets or sets the format string.
    /// </summary>
    public string? FormatString { get; set; }

    /// <summary>
    /// Gets or sets the formatted display string.
    /// </summary>
    public string? FormattedValue { get; set; }

    /// <summary>
    /// Gets or sets whether formatting has been applied.
    /// Set to true if custom formatting was applied.
    /// </summary>
    public bool FormattingApplied { get; set; }

    private static string? FormatValue(object? value, string? format)
    {
        if (value == null)
            return string.Empty;

        if (string.IsNullOrEmpty(format))
        {
            if (value is double d)
            {
                if (d == Math.Floor(d) && !double.IsInfinity(d))
                    return d.ToString("0");
                return d.ToString("0.##");
            }
            return value.ToString();
        }

        try
        {
            var fmt = format.Contains("{0") ? format : "{0:" + format + "}";
            return string.Format(fmt, value);
        }
        catch
        {
            return value.ToString();
        }
    }
}

/// <summary>
/// Provides data for the <see cref="PivotTable.CellPainting"/> event.
/// Allows custom drawing of a data cell.
/// </summary>
public class PivotTableCellPaintingEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="PivotTableCellPaintingEventArgs"/>.
    /// </summary>
    public PivotTableCellPaintingEventArgs(
        Graphics graphics,
        Rectangle cellBounds,
        int rowIndex, int columnIndex,
        string? displayText,
        Color foreColor, Color backColor,
        bool isSelected, bool isTotalCell)
    {
        Graphics = graphics;
        CellBounds = cellBounds;
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
        DisplayText = displayText;
        ForeColor = foreColor;
        BackColor = backColor;
        IsSelected = isSelected;
        IsTotalCell = isTotalCell;
    }

    /// <summary>
    /// Gets the graphics surface to draw on.
    /// </summary>
    public Graphics Graphics { get; }

    /// <summary>
    /// Gets the bounds of the cell being painted.
    /// </summary>
    public Rectangle CellBounds { get; }

    /// <summary>
    /// Gets the row index of the cell.
    /// </summary>
    public int RowIndex { get; }

    /// <summary>
    /// Gets the column index of the cell.
    /// </summary>
    public int ColumnIndex { get; }

    /// <summary>
    /// Gets or sets the display text for the cell.
    /// </summary>
    public string? DisplayText { get; set; }

    /// <summary>
    /// Gets or sets the foreground color.
    /// </summary>
    public Color ForeColor { get; set; }

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public Color BackColor { get; set; }

    /// <summary>
    /// Gets whether the cell is currently selected.
    /// </summary>
    public bool IsSelected { get; }

    /// <summary>
    /// Gets whether this cell is a total (subtotal or grand total).
    /// </summary>
    public bool IsTotalCell { get; }

    /// <summary>
    /// Gets or sets whether the cell painting is handled.
    /// Set to true to skip default rendering.
    /// </summary>
    public bool Handled { get; set; }
}

/// <summary>
/// Provides data for the <see cref="PivotTable.CellClick"/> event.
/// </summary>
public class PivotTableCellClickEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="PivotTableCellClickEventArgs"/>.
    /// </summary>
    public PivotTableCellClickEventArgs(int rowIndex, int columnIndex, bool isTotalCell)
    {
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
        IsTotalCell = isTotalCell;
    }

    /// <summary>
    /// Gets the row index of the clicked cell.
    /// </summary>
    public int RowIndex { get; }

    /// <summary>
    /// Gets the column index of the clicked cell.
    /// </summary>
    public int ColumnIndex { get; }

    /// <summary>
    /// Gets whether the clicked cell is a total cell.
    /// </summary>
    public bool IsTotalCell { get; }
}

/// <summary>
/// Provides data for the <see cref="PivotTable.DrilldownToggled"/> event.
/// Fired when a drilldown node is expanded or collapsed.
/// </summary>
public class PivotTableDrilldownEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="PivotTableDrilldownEventArgs"/>.
    /// </summary>
    public PivotTableDrilldownEventArgs(int rowFieldIndex, string nodePath, string nodeValue, bool isExpanded)
    {
        RowFieldIndex = rowFieldIndex;
        NodePath = nodePath;
        NodeValue = nodeValue;
        IsExpanded = isExpanded;
    }

    /// <summary>
    /// Gets the index of the row field that was toggled.
    /// </summary>
    public int RowFieldIndex { get; }

    /// <summary>
    /// Gets the full path of the node (pipe-separated hierarchy).
    /// </summary>
    public string NodePath { get; }

    /// <summary>
    /// Gets the display value of the node.
    /// </summary>
    public string NodeValue { get; }

    /// <summary>
    /// Gets whether the node is now expanded.
    /// </summary>
    public bool IsExpanded { get; }
}

/// <summary>
/// Provides data for the <see cref="PivotTable.DrillThrough"/> event.
/// Fired when a data cell is double-clicked to show detail records.
/// </summary>
public class PivotTableDrillThroughEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="PivotTableDrillThroughEventArgs"/>.
    /// </summary>
    public PivotTableDrillThroughEventArgs(
        int rowIndex, int columnIndex,
        IReadOnlyList<object> detailRecords)
    {
        RowIndex = rowIndex;
        ColumnIndex = columnIndex;
        DetailRecords = detailRecords;
    }

    /// <summary>
    /// Gets the row index of the drilled cell.
    /// </summary>
    public int RowIndex { get; }

    /// <summary>
    /// Gets the column index of the drilled cell.
    /// </summary>
    public int ColumnIndex { get; }

    /// <summary>
    /// Gets the list of detail records that make up the aggregated value.
    /// </summary>
    public IReadOnlyList<object> DetailRecords { get; }

    /// <summary>
    /// Gets or sets whether the drill-through action has been handled.
    /// Set to true to suppress the default drill-through behavior.
    /// </summary>
    public bool Handled { get; set; }
}
