using System;
using OldSchoolForms.Ui.Core;
using OldSchoolForms.Ui.Rendering;

namespace OldSchoolForms.Ui.Controls.Advanced;

/// <summary>
/// Provides data for the <see cref="DataGridView.CellPainting"/> event.
/// </summary>
public class DataGridViewCellPaintingEventArgs : DataGridViewCellEventArgs
{
    /// <summary>
    /// Gets the graphics object used for painting.
    /// </summary>
    public Graphics Graphics { get; }

    /// <summary>
    /// Gets or sets the bounds of the cell being painted.
    /// </summary>
    public Rectangle CellBounds { get; }

    /// <summary>
    /// Gets or sets a value that indicates whether the event has been handled.
    /// If set to true, the default painting logic is skipped.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="DataGridViewCellPaintingEventArgs"/>.
    /// </summary>
    /// <param name="columnIndex">The column index of the cell.</param>
    /// <param name="rowIndex">The row index of the cell.</param>
    /// <param name="graphics">The graphics object used for painting.</param>
    /// <param name="cellBounds">The bounds of the cell.</param>
    public DataGridViewCellPaintingEventArgs(int columnIndex, int rowIndex, Graphics graphics, Rectangle cellBounds)
        : base(columnIndex, rowIndex)
    {
        Graphics = graphics;
        CellBounds = cellBounds;
        Handled = false;
    }
}
